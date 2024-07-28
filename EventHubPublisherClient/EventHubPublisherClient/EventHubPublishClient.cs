using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace EventHubPublisherClient
{
    public class EventHubPubClient
    {
        private EventHubProducerClient _ehProducerClient;
        private SendEventOptions _sendEventOptions;
        private string _defaultPartitionId = "0";

        public EventHubPubClient(string eventHubNamespace, string eventHubName, string defaultPartitionId)
        {
            if (string.IsNullOrWhiteSpace(eventHubNamespace) || string.IsNullOrWhiteSpace(eventHubName))
            {
                throw new ArgumentException("Event Hub Namespace and Hub Name must be provided.");
            }
            _ehProducerClient = new EventHubProducerClient(eventHubNamespace, eventHubName);

            if (!string.IsNullOrWhiteSpace(defaultPartitionId))
            {
                _defaultPartitionId = defaultPartitionId;
            }
            _sendEventOptions = new SendEventOptions { PartitionId = _defaultPartitionId };
        }

        public async Task<bool> SendEventAsync(string serialisedObject, string eventType, string partitionId)
        {
            if (string.IsNullOrWhiteSpace(serialisedObject) || string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentException("Serialised Object and Event Type must be provided.");
            }

            _sendEventOptions.PartitionId = (!string.IsNullOrWhiteSpace(partitionId) ? partitionId : _defaultPartitionId);

            try
            {
                var eventBody = new BinaryData(serialisedObject);
                var eventData = new EventData(eventBody);
                eventData.Properties["EventType"] = eventType; // Support consumer filtering on EventType
                var eventList = new List<EventData> { eventData };

                // The Event Hub will throw an exeption if the event is not sent
                await _ehProducerClient.SendAsync(eventList, _sendEventOptions);
                return true;
            }
            catch (Exception)
            {
                return false; 
            }
        }

    }
}
