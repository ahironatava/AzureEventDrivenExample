using CommonModels;

namespace Repository.Interfaces
{
    public interface IUserRequestService
    {
        public Task<Dictionary<string, UserRequest>> GetUserRequestDictionary();
        
        public Task<UserRequest> GetUserRequest(string id);

        public Task<int> AddUserRequest(UserRequest userRequest);
    }
}
