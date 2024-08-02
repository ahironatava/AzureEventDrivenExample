using Newtonsoft.Json;

namespace CommonModels
{
    public class UserRequest
    {
        public string? RequestId { get; set; }
        public string? UserName { get; set; }
        public Transaction UserTransaction { get; set; }

        public UserRequest()
        {
            UserTransaction = new Transaction();
        }

        public UserRequest(string json)
        {
            UserRequest userRequest = JsonConvert.DeserializeObject<UserRequest>(json);
            RequestId = userRequest.RequestId;
            UserName = userRequest.UserName;
            UserTransaction = userRequest.UserTransaction;
        }
    }
}
