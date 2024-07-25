using CommonModels;
using Newtonsoft.Json.Linq;
using ProcConfigBuilder.Interfaces;
using EventGridPublishClient;
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

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repo_url"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _repoClient = new RepoClient(repoUrl);
            _logger = logger;
        }

        public async Task<bool> CreateAndPublishProcConfigFile(GridEvent<dynamic> gridEvent)
        {
            // Create the configuration file content; subsequent processing requires this to succeed
            var config = CreateProcConfig(gridEvent);
            if (config == null)
            {
                string errMsg = "config is null";
                _logger.LogError(errMsg);
                return false;
            }

            // If the RequestId is unavailable further processing is futile
            if((config.UserRequest == null) || string.IsNullOrWhiteSpace(config.UserRequest.RequestId))
            {
                string errMsg = "unable to determine the RequestId";
                _logger.LogError(errMsg);
                return false;
            }

            // Save the configuration file to the repository.
            (bool saved, string errString) = await _repoClient.SaveProcConfigAsync(config);
            if (!saved)
            {
                _logger.LogError($"Failed to save config file: {errString}");
                return false;
            }

            // Publish a ProcConfigCreatedEvent to the EventGrid

            // Event State Transfer - the whole configuration file is sent
            //var published = await _eventGridPubClient.PublishEventGridEvent(config.UserRequest.RequestId, "ProcConfigCreatedEvent", config);

            // Event Notification - only the umbrella identifier and event type is sent
            var published = await _eventGridPubClient.PublishEventGridEvent(config.UserRequest.RequestId, "ProcConfigCreatedEvent", string.Empty);
            return (published == 200);
        }

        private ProcConfig? CreateProcConfig(GridEvent<dynamic> gridEvent)
        {
            ProcConfig procConfig = null;
            try
            {
                // Use the UserRequest UserName to access the configuration data for the user
                var userName = gridEvent.Data.UserRequest.UserName;
                var userProcParameters = GetUserProcParameters(userName);

                procConfig = new ProcConfig
                {
                    UserRequest = gridEvent.Data.UserRequest,
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
