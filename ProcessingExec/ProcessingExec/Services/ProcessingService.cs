using CommonModels;
using EventHubPublisherClient;
using FakeDatabase;
using ProcessingExec.Interfaces;
using RepositoryClient;
using System.Text.Json;

namespace ProcessingExec.Services
{
    public class ProcessingService : IProcessingService
    {
        private readonly IConfiguration _configuration; // Configuration information
        private readonly RepoClient _repoClient;        // System artefacts 

        private readonly EventHubPubClient _eventHubPubClient;
        private string _defaultPartitionId = "0";

        private readonly FakeDatabaseClient _fakeDatabaseClient;

        private readonly ILogger<ProcessingService> _logger;


        public ProcessingService(IConfiguration configuration, ILogger<ProcessingService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repo_url"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _repoClient = new RepoClient(repoUrl);

            // Create the EventHubPubClient (it performs null checks on the parameters)
            string? hubNamespace = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];
            string? hubpartitionid = _configuration["eh_partition_id"];
            _eventHubPubClient = new EventHubPubClient(hubNamespace, hubName, hubpartitionid);

            // Create the FakeDatabase
            _fakeDatabaseClient = new FakeDatabaseClient();
        }

        public async Task<int> ApplyConfiguredProcessing(string requestId)
        {
            ProcConfig? procConfig = null;
            ProcessingResults procResults = new ProcessingResults()
            {
                RequestId = requestId
            };

            procResults.StatusMessage = string.Empty;
            procResults.ProcessingSuccessful = false;

            string errMsg = string.Empty;
            
            // Get the ProcConfig file
            
            // Hard-coded delay to allow the repository to save the file before attempting to access it
            await Task.Delay(5000);

            (string errString, procConfig) = await _repoClient.GetProcConfigAsync(requestId);
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

                _logger.LogInformation($"procConfig.UserRequest.UserName = {procConfig.UserRequest.UserName}");
                _logger.LogInformation($"procConfig.UserRequest.UserTransaction.StockName = {procConfig.UserRequest.UserTransaction.StockName}");
                _logger.LogInformation($"procConfig.UserRequest.UserTransaction.Quantity = {procConfig.UserRequest.UserTransaction.Quantity}");

                // Orchestrate the required processing
                var processingResults = OrchestrateProcessing(procConfig, procResults);
                procResults.ProcessingSuccessful = (string.IsNullOrWhiteSpace(processingResults.StatusMessage)) ? true : false;
            }

            // Save the results to the repository
            _logger.LogInformation("Saving processing results to repository");
            (bool saveSuccessful, string saveErrMsg) = await _repoClient.SaveProcResultAsync(procResults);
            if(!saveSuccessful)
            {
                errMsg = $"Error saving processing results for request {requestId}";
                _logger.LogError(saveErrMsg);
                procResults.ProcessingSuccessful = false;
            }

            // Send ProcessingCompleteEvent to EventHub
            _logger.LogInformation("Sending notification to Event Hub");
            var notificationBody = new ClientNotification()
            {
                RequestId = requestId,
                ProcessingSuccessful = procResults.ProcessingSuccessful
            };
            if( await SendNotificationtoEventHub(notificationBody))
            {
                return StatusCodes.Status200OK;
            }
            else
            {
                return StatusCodes.Status500InternalServerError;
           }
        }

        private ProcessingResults OrchestrateProcessing(ProcConfig procConfig, ProcessingResults procResults)
        {
            int balanceBefore = 0;
            int balanceAfter = 0;
            bool processingSuccessful = false;
            procResults.StatusMessage = string.Empty;

            try
            {
                balanceBefore = _fakeDatabaseClient.GetStockBalance(procConfig.UserRequest.UserName, procConfig.UserRequest.UserTransaction.StockName);
                processingSuccessful = ApplyProcessing(procConfig);
                balanceAfter = _fakeDatabaseClient.GetStockBalance(procConfig.UserRequest.UserName, procConfig.UserRequest.UserTransaction.StockName);

                _logger.LogInformation("\nProcessing complete");
                _logger.LogInformation($"balanceBefore = {balanceBefore}");
                _logger.LogInformation($"processingSuccessful = {processingSuccessful}");
                _logger.LogInformation($"balanceAfter = {balanceAfter}");

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
                        _fakeDatabaseClient.AddStockBalance(userRequest.UserName, userRequest.UserTransaction.StockName, userRequest.UserTransaction.Quantity);
                        break;
                    case "sell":
                        _fakeDatabaseClient.DeductStockBalance(userRequest.UserName, userRequest.UserTransaction.StockName, userRequest.UserTransaction.Quantity);
                        break;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error updating balance for RequestID: {userRequest.RequestId}");
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

            bool sent = await _eventHubPubClient.SendEventAsync(notificationAsJson, eventType, partitionId);
            if (!sent)
            {
                _logger.LogError($"Error sending notification to EventHub for RequestID: {notificationBody.RequestId}");
            }
            return sent;
        }
    }
}
