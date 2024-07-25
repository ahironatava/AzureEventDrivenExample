using CommonModels;
using EventHubPublisherClient;
using FacadeApi.Interfaces;
using RepositoryClient;
using System.Text.Json;

namespace FacadeApi.Services
{
    public class UserRequestService : IUserRequestService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UserRequestService> _logger;

        private EventHubPubClient _eventHubPubClient;
        private string _defaultPartitionId = "0";

        private RepoClient _repoClient;

        public UserRequestService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Create the EventHubPubClient (it performs null checks on the parameters)
            string? hubNamesapce = _configuration["ehns_connstring"];
            string? hubName = _configuration["eh_name"];
            string? hubpartitionid = _configuration["eh_partition_id"];
            _eventHubPubClient = new EventHubPubClient(hubNamesapce, hubName, hubpartitionid);  

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repo_url"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _repoClient = new RepoClient(repoUrl);
        }

        public async Task<bool> ProcessRequest(Transaction transaction, string userName)
        {
            UserRequest userRequest = new UserRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                UserName = userName,
                UserTransaction = transaction
            };

            // Save the User Request to the Repository. If this fails there is no benefit to continuing
            (bool savedSuccessfully, string errMsg) = await _repoClient.SaveUserRequestAsync(userRequest);
            if(!savedSuccessfully)
            {
                _logger.LogError($"Failed to save User Request to Repository: {errMsg}");
                return false;
            }

            return await SendRequestToEventHub(userRequest);
        }

        private async Task<bool> SendRequestToEventHub(UserRequest userRequest)
        {
            // Sent the event to Event Hub using the  Event State Transfer pattern
            // i.e. sending the data in the event body, as the data size is small
            var requestAsJson = JsonSerializer.Serialize(userRequest);
            var eventType = "UserRequestEvent"; // Support consumer filtering on EventType

            // Select the partition acording to the transaction type, then publish the event
            var partitionId = (string.Equals(userRequest.UserTransaction.TransactionType.ToLower(), "sell") ? "1" : _defaultPartitionId);

            bool sent = await _eventHubPubClient.SendEventAsync(requestAsJson, eventType, partitionId);
            if(!sent)
            {
                _logger.LogError("Failed to send UserRequestEvent to Event Hub.");
            }

            return sent;
        }
    }
}
