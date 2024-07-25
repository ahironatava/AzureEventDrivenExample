using Azure;
using Azure.Messaging.EventGrid;

namespace EventGridPublishClient
{
    public class EventGridPubClient
    {
        private readonly EventGridPublisherClient _client;

        public EventGridPubClient(string topicEndpoint, string topicAccessKey)
        {
            _client = new EventGridPublisherClient(
                new Uri(topicEndpoint),
                new AzureKeyCredential(topicAccessKey));
        }

        public async Task<int> PublishEventGridEvent<T>(string subject, string type, T objectToSend)
        {
            // Publish the Event Grid Event to the Event Grid.
            // Intentionally not stating the dataSerializationType so that System.Object.GetType will be applied to the objectToSend
            // (https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Messaging.EventGrid/4.0.0/api/Azure.Messaging.EventGrid/Azure.Messaging.EventGrid.EventGridEvent.html)
            EventGridEvent eventToPublish = new EventGridEvent(subject, type, "1.0", objectToSend);
            List<EventGridEvent> eventList = new List<EventGridEvent> { eventToPublish };
            var response = await _client.SendEventsAsync(eventList);
            return response.Status;
        }

        public void PublishCloudEvent(string value)
        {
            // Publish the CloudEvent 1.0 schema event to the Event Grid.
        }

    }
}
