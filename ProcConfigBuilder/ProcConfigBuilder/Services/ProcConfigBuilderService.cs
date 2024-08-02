using CommonModels;
using EventGridPublishClient;
using ProcConfigBuilder.Interfaces;
using RepositoryClient;

namespace ProcConfigBuilder.Services
{
    public class ProcConfigBuilderService : IProcConfigBuilderService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProcConfigBuilderService> _logger;
        private readonly EventGridPubClient _eventGridPubClient;
        private readonly RepoClient _repoClient;


        public ProcConfigBuilderService(IConfiguration configuration, ILogger<ProcConfigBuilderService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            string? TopicEndpoint = _configuration["EventGridTopicEndpoint"];
            string? TopicKey = _configuration["EventGridTopicKey"]; ;

            if (string.IsNullOrWhiteSpace(TopicEndpoint) || string.IsNullOrWhiteSpace(TopicKey))
            {
                throw new ArgumentException(" Event Grid Topic Endpoint and Key must be provided.");
            }
            _eventGridPubClient = new EventGridPubClient(TopicEndpoint, TopicKey);

            _logger.LogInformation("EventGridPubClient created");

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repo_url"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _logger.LogInformation($"repoUrl read as {repoUrl}.");

            _repoClient = new RepoClient(repoUrl);
            _logger.LogInformation("_repoClient created");
        }

        public async Task<int> CreateAndPublishProcConfigFile(UserRequest userRequest)
        {
            // Create the configuration file content; subsequent processing requires this to succeed
            var config = CreateProcConfig(userRequest);
            if (config == null)
            {
                return StatusCodes.Status500InternalServerError;
            }

            _logger.LogInformation("ProcConfig created");
            _logger.LogInformation($"UserRequest.UserName: {userRequest.UserName}");
            _logger.LogInformation($"UserRequest.RequestId: {userRequest.RequestId}");
            _logger.LogInformation($"UserRequest.UserTransaction.TransactionType: {userRequest.UserTransaction.TransactionType}");
            _logger.LogInformation($"UserRequest.UserTransaction.StockName: {userRequest.UserTransaction.StockName}");
            _logger.LogInformation($"UserRequest.UserTransaction.Quantity: {userRequest.UserTransaction.Quantity}");
            _logger.LogInformation($"UserProcParameters.AccountIsLocked: {config.UserProcParameters.AccountIsLocked}");
            _logger.LogInformation($"UserProcParameters.Restrictions[0]: {config.UserProcParameters.Restrictions[0]}");

            // Save the configuration file to the repository.
            (bool saved, string errString) = await _repoClient.SaveProcConfigAsync(config);
            if (!saved)
            {
                _logger.LogError($"Failed to save config file: {errString}");
                return StatusCodes.Status500InternalServerError;
            }

            // Publish a ProcConfigCreatedEvent to the EventGrid
            // Event Notification - only the request identifier and event type is sent
            return await _eventGridPubClient.PublishEventGridEvent(config.UserRequest.RequestId, "ProcConfigCreatedEvent", string.Empty);
        }

        private ProcConfig? CreateProcConfig(UserRequest userRequest)
        {
            ProcConfig procConfig = null;
            try
            {
                // Use the UserRequest UserName to access the configuration data for the user
                var userName = userRequest.UserName;
                var userProcParameters = GetUserProcParameters(userName);

                procConfig = new ProcConfig
                {
                    UserRequest = userRequest,
                    UserProcParameters = userProcParameters
                };
            }
            catch(Exception e)
            {
                _logger.LogError(e, "Error creating ProcConfig");
            }
            return procConfig;
        }

        private UserProcParameters GetUserProcParameters(string userName)
        {
            // This would be a call to a service, database or other data store to get the configuration data
            // Hard-coding a fake, for now
            return new UserProcParameters
            {
                AccountIsLocked = false,
                AccessibleStocks = new Dictionary<string, bool>
                {
                    { "AAPL", true },
                    { "MSFT", true },
                    { "AMZN", false }
                },
                Restrictions = new List<string>
                {
                    "No short selling",
                    "No options trading"
                }
            };
        }
    }
}
