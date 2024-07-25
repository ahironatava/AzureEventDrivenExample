using CommonModels;

namespace FacadeApi.Interfaces
{
    public interface IUserRequestService
    {
        public Task<bool> ProcessRequest(Transaction transaction, string userName);
    }
}
