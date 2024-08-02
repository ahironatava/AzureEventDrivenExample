using CommonModels;
using Repository.Interfaces;

namespace Repository.Services
{
    public class UserRequestService : IUserRequestService
    {
        private readonly Dictionary<string, UserRequest> _userRequests;
        public UserRequestService()
        {
            _userRequests = new Dictionary<string, UserRequest>();
        }

        public async Task<Dictionary<string, UserRequest>> GetUserRequestDictionary()
        {
           return _userRequests;
        }

        public async Task<UserRequest> GetUserRequest(string id)
        {
            UserRequest userRequest = null;
            try
            {
                userRequest = _userRequests[id];
            }
            catch (KeyNotFoundException)
            {
                //
            }
            return userRequest;
        }

        public async Task<int> AddUserRequest(UserRequest userRequest)
        {
            try
            {
                _userRequests.Add(userRequest.RequestId, userRequest);
            }
            catch (ArgumentException)
            {
                return 1;
            }
            return 0;
        }


    }
}
