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
            if (string.IsNullOrWhiteSpace(eventHubNamespace))
            {
                throw new ArgumentException("Event Hub Namespace must be provided.");
            }

            // NOTE: if the namespace includes the Event Hub name, the eventHubName parameter can be ignored
            // otherwise, it must be appended.
            if (!eventHubNamespace.Contains("EntityPath"))
            {
                if (string.IsNullOrWhiteSpace(eventHubName))
                {
                    throw new ArgumentException("Event Hub Name must be provided if it is not included in the Namespace");
                }
                else
                {
                    eventHubNamespace += $";EntityPath={eventHubName}";
                }
            }
            _ehProducerClient = new EventHubProducerClient(eventHubNamespace);

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
