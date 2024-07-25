using CommonModels;

namespace ClientNotifier.Interfaces
{
    public interface IClientNotifierService
    {
        public Task<bool> NotifyClient(GridEvent<dynamic> gridEvent);
    }
}
