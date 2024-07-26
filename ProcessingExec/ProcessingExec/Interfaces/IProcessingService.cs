using CommonModels;

namespace ProcessingExec.Interfaces
{
    public interface IProcessingService
    {
        public Task<int> ApplyConfiguredProcessing(GridEvent<dynamic> gridEvent);
    }
}
