using CommonModels;

namespace Repository.Interfaces
{
    public interface IProcResultService
    {
        public Task<ProcessingResults> GetProcessingResults(string id);

        public Task<int> AddProcessingResults(ProcessingResults processingResults);
    }
}

