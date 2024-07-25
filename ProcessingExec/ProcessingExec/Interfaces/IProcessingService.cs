using CommonModels;

namespace ProcessingExec.Interfaces
{
    public interface IProcessingService
    {
        public Task<bool> ApplyConfiguredProcessing(GridEvent<dynamic>? gridEvent);
    }
}
