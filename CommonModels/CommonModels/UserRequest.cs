namespace CommonModels
{
    public class UserRequest
    {
        public string? RequestId { get; set; }
        public string? UserName { get; set; }
        public Transaction UserTransaction { get; set; }
    }
}
