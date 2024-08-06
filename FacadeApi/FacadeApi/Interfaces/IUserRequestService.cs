using CommonModels;

namespace FacadeApi.Interfaces
{
    public interface IUserRequestService
    {
        public Task<(bool, string)> ProcessRequest(Transaction transaction, string userName);
    }
}
