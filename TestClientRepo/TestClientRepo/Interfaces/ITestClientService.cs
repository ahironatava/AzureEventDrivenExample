using CommonModels;

namespace TestClientRepo.Interfaces
{
    public interface ITestClientService
    {
        public Task<Dictionary<string, UserRequest>?> GetUserRequestDictionary();

        public Task<UserRequest?> GetUserRequest(string requestId);

        public Task<bool> AddUserRequest(UserRequest userRequest);



        public Task<Dictionary<string, ProcConfig>?> GetProcConfigDictionary();

        public Task<ProcConfig> GetProcConfig(string requestId);

        public Task<bool> AddProcConfig(ProcConfig procConfig);


        public Task<Dictionary<string, ProcessingResults>?> GetProcessingResultsDictionary();

        public Task<ProcessingResults> GetProcessingResults(string requestId);

        public Task<bool> AddProcessingResults(ProcessingResults processingResults);


    }
}
