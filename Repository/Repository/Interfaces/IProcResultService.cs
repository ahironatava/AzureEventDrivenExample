using CommonModels;

namespace Repository.Interfaces
{
    public interface IProcResultService
    {
        public Dictionary<string, ProcessingResults> GetProcessingResultsDictionary();

        public Task<ProcessingResults> GetProcessingResults(string id);

        public Task<int> AddProcessingResults(ProcessingResults processingResults);
    }
}

