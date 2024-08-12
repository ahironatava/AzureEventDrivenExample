using CommonModels;
using Newtonsoft.Json;
using RepositoryClient;
using TestClientRepo.Interfaces;

namespace TestClientRepo.Services
{
    public class TestClientService : ITestClientService
    {
        private readonly IConfiguration _configuration;
        private readonly RepoClient _repoClient;

        public TestClientService(IConfiguration configuration)
        {
            _configuration = configuration;

            // Initialise the Repository client interface
            string? repoUrl = _configuration["repoUrl"];
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                throw new ArgumentException("Repository URL must be provided.");
            }
            _repoClient = new RepoClient(repoUrl);
        }

        // UserRequest
        public async Task<Dictionary<string, UserRequest>?> GetUserRequestDictionary()
        {
            (string errMsg, Dictionary<string, UserRequest> dictionary) = await _repoClient.GetUserRequestDictionary();
            return dictionary;
        }

        public async Task<UserRequest?> GetUserRequest(string id)
        {
            (string errMsg, UserRequest userRequest) = await _repoClient.GetUserRequestAsync(id);
            return userRequest;
        }

        public async Task<bool> AddUserRequest(UserRequest userRequest)
        {
            (bool success, string errMsg) = await _repoClient.SaveUserRequestAsync(userRequest);
            return success;
        }

        // ProcConfig
        public async Task<Dictionary<string, ProcConfig>?> GetProcConfigDictionary()
        {
            (string errMsg, Dictionary<string, ProcConfig> dictionary) = await _repoClient.GetProcConfigDictionary();
            return dictionary;
        }

        public async Task<ProcConfig?> GetProcConfig(string id)
        {
            (string errMsg, ProcConfig procConfig) = await _repoClient.GetProcConfigAsync(id);
            return procConfig;
        }

        public async Task<bool> AddProcConfig(ProcConfig procConfig)
        {
            (bool success, string errMsg) = await _repoClient.SaveProcConfigAsync(procConfig);
            return success;
        }

        // ProcessingResults
        public async Task<Dictionary<string, ProcessingResults>?> GetProcessingResultsDictionary()
        {
            (string errMsg, Dictionary<string, ProcessingResults> dictionary) = await _repoClient.GetProcessingResultsDictionary();
            return dictionary;
        }

        public async Task<ProcessingResults?> GetProcessingResults(string id)
        {
            (string errMsg, ProcessingResults processingResults) = await _repoClient.GetProcResultAsync(id);
            return processingResults;
        }

        public async Task<bool> AddProcessingResults(ProcessingResults processingResults)
        {
            (bool success, string errMsg) = await _repoClient.SaveProcResultAsync(processingResults);
            return success;
        }

    }
}

