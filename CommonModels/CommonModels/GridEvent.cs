namespace CommonModels
{
    public class GridEvent<T> where T : class
    {
        // https://learn.microsoft.com/en-us/azure/event-grid/event-schema
        // https://learn.microsoft.com/en-us/dotnet/api/azure.messaging.eventgrid.eventgridevent?view=azure-dotnet
        public string Id { get; set; }
        public string EventType { get; set; }
        public string Subject { get; set; }
        public DateTime EventTime { get; set; }
        public T Data { get; set; }
        public string Topic { get; set; }
    }
}