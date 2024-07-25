using Azure.Core;
using CommonModels;
using EventHubPublisherClient;
using FakeDatabase;
using ProcessingExec.Interfaces;
using RepositoryClient;
using System.ComponentModel;
using System.Text.Json;

namespace ProcessingExec.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IConfiguration _configuration; // Configuration information
        private readonly IFakeDatabase _fakeDatabase;   // Processing Results
        private readonly RepoClient _repoClient;        // System artefacts 

        private readonly EventHubPubClient _eventHubPubClient;
        private string _defaultPartitionId = "0";

        private readonly ILogger<ProcessingService> _logger;


        public ProcessingService(IConfiguration configuration, IFakeDatabase fakeDatabase)
        {
            _configuration = configuration;
            _fakeDatabase = fakeDatabase;

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repo_url"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _repoClient = new RepoClient(repoUrl);

            // Create the EventHubPubClient (it performs null checks on the parameters)
            string? hubNamesapce = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];
            string? hubpartitionid = _configuration["eh_partition_id"];
            _eventHubPubClient = new EventHubPubClient(hubNamesapce, hubName, hubpartitionid);
        }

        public async Task<bool> ApplyConfiguredProcessing(GridEvent<dynamic>? gridEvent)
        {
            ProcConfig? procConfig = null;
            ProcessingResults procResults = new ProcessingResults();

            procResults.StatusMessage = string.Empty;
            procResults.ProcessingSuccessful = false;

            string errMsg = string.Empty;

            // Get the Request ID
            var dynamicRequestId = gridEvent?.Data?.Id;
            if (string.IsNullOrWhiteSpace(dynamicRequestId))
            {
                _logger.LogError("Request Id is missing.");
                return false;
            }
            string requestId = gridEvent.Data.Id.ToString();
            procResults.RequestId = requestId;

            // Get the ProcConfig file
            (string errString, ProcConfig procConfigRead) = await _repoClient.GetProcConfigAsync(requestId);
            if (procConfig == null)
            {
                errMsg = $"ProcConfig file is missing for request {requestId}";
                _logger.LogError(errMsg);
                procResults.StatusMessage = errMsg;
            }
            else
            {
                procResults.UserName = procConfig.UserRequest.UserName;
                procResults.StockName = procConfig.UserRequest.UserTransaction.StockName;
                procResults.Quantity = procConfig.UserRequest.UserTransaction.Quantity;
            }

            // Orchestrate the required processing
            if (procConfig != null)
            {
                var processingResults = OrchestrateProcessing(procConfig, procResults);
                procResults.ProcessingSuccessful = (string.IsNullOrWhiteSpace(processingResults.StatusMessage)) ? false : true;
            }

            // Send ProcessingCompleteEvent to EventHub
            var notificationBody = new ClientNotification()
            {
                RequestId = requestId,
                ProcessingSuccessful = procResults.ProcessingSuccessful
            };
            return await SendNotificationtoEventHub(notificationBody);
        }

        private ProcessingResults OrchestrateProcessing(ProcConfig procConfig, ProcessingResults procResults)
        {
            int balanceBefore = 0;
            int balanceAfter = 0;
            bool processingSuccessful = false;

            try
            {
                balanceBefore = _fakeDatabase.GetStockBalance(procConfig.UserRequest.UserName, procConfig.UserRequest.UserTransaction.StockName);
                processingSuccessful = ApplyProcessing(procConfig);
                balanceAfter = _fakeDatabase.GetStockBalance(procConfig.UserRequest.UserName, procConfig.UserRequest.UserTransaction.StockName);

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error processing request");
                procResults.StatusMessage = e.Message;
            }

            procResults.BalanceBefore = balanceBefore;
            procResults.BalanceAfter = balanceAfter;

            return procResults;
        }

        private bool ApplyProcessing(ProcConfig procConfig)
        {
            // Apply the processing in accordance with configuration for the user
            UserProcParameters userprocParameters = procConfig.UserProcParameters;
            UserRequest userRequest = procConfig.UserRequest;

            if (RequestIsCompliant(userRequest, userprocParameters))
            {
                return UpdateBalance(userRequest, userprocParameters);
            }
            else
            {
                return false;
            }
        }

        private bool RequestIsCompliant(UserRequest userRequest, UserProcParameters userprocParameters)
        {
            // This method checks if the user request is compliant with the user's status and business rules.

            // This is a simple example of a compliance check. In a real-world scenario, this method would
            // check the user's request against the user's processing parameters to determine if the request
            // is compliant.

            if (userprocParameters.AccountIsLocked)
            {
                _logger.LogInformation("User account is locked");
                return false;
            }

            if (userRequest.UserTransaction.TransactionType.ToLower() == "buy")
            {
                // Check business rules here ...
                return true;
            }
            else if (userRequest.UserTransaction.TransactionType.ToLower() == "sell")
            {
                // Check business rules here ...
                return true;
            }
            else
            {
                _logger.LogInformation("Invalid transaction type");
                return false;
            }
        }

        private bool UpdateBalance(UserRequest userRequest, UserProcParameters userprocParameters)
        {
            if (string.IsNullOrWhiteSpace(userRequest.UserName)
                || userRequest.UserTransaction == null
                || string.IsNullOrWhiteSpace(userRequest.UserTransaction.StockName)
                || userRequest.UserTransaction.Quantity <= 0)
            {
                _logger.LogError("Invalid input parameters");
                return false;
            }

            try
            {
                switch (userRequest.UserTransaction.TransactionType.ToLower())
                {
                    case "buy":
                        _fakeDatabase.AddStockBalance(userRequest.UserName, userRequest.UserTransaction.StockName, userRequest.UserTransaction.Quantity);
                        break;
                    case "sell":
                        _fakeDatabase.DeductStockBalance(userRequest.UserName, userRequest.UserTransaction.StockName, userRequest.UserTransaction.Quantity);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error updating balance");
                return false;
            }

            return true;
        }

        private async Task<bool> SendNotificationtoEventHub(ClientNotification notificationBody)
        {
            var notificationAsJson = JsonSerializer.Serialize(notificationBody);
            var eventType = "ProcessingCompleteEvent"; // Support consumer filtering on EventType

            // Select the partition acording to the transaction type, then publish the event
            var partitionId = _defaultPartitionId;

            return await _eventHubPubClient.SendEventAsync(notificationAsJson, eventType, partitionId);
        }
    }
}
