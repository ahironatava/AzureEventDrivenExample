using CommonModels;
using Repository.Interfaces;

namespace Repository.Services
{
    public class ProcResultService : IProcResultService
    {
        private readonly Dictionary<string, ProcessingResults> _processingResults;
        public ProcResultService()
        {
            _processingResults = new Dictionary<string, ProcessingResults>();
        }

        public async Task<ProcessingResults> GetProcessingResults(string id)
        {
            ProcessingResults processingResults = null;
            try
            {
                processingResults = _processingResults[id];
            }
            catch (KeyNotFoundException)
            {
                //
            }
            return processingResults;
        }

        public async Task<int> AddProcessingResults(ProcessingResults processingResults)
        {
            try
            {
                _processingResults.Add(processingResults.RequestId, processingResults);
            }
            catch (ArgumentException)
            {
                return 1;
            }
            return 0;
        }
    }
}
