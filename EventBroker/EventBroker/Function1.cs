using CommonModels;
using EventGridPublishClient;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace EventBroker
{
    public class Function1
    {
        private readonly IConfiguration _configuration;

        private readonly ILogger<Function1> _logger;

        private readonly EventGridPubClient _reqEventGridClient; // Requests received
        private readonly EventGridPubClient _resEventGridClient; // Results to send

        private enum EventType
        {
            UnexpectedEventType,
            UserRequestEvent,
            ProcessingCompleteEvent
        }

        public Function1(IConfiguration configuration, ILogger<Function1> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _reqEventGridClient = CreateEventGridPubClient("request");
            _resEventGridClient = CreateEventGridPubClient("result");
        }

        [Function(nameof(EventHubFunction))]
        public async Task EventHubFunction(
            [EventHubTrigger("src", Connection = "EventHubConnection")] string[] input,
            FunctionContext context)
        {
            var eventData = context.BindingContext.BindingData;
            EventType eventType = GetEventType(eventData);

            switch (eventType)
            {
                case EventType.UserRequestEvent:
                    _logger.LogInformation("Processing UserRequestEvent");
                    await ProcessUserRequestEvent(input[0]);
                    break;
                case EventType.ProcessingCompleteEvent:
                    _logger.LogInformation("Processing ProcessingCompleteEvent");
                    await ProcessProcessingCompleteEvent(input[0]);
                    break;
                default:
                    _logger.LogError($"Unexpected event type {eventType}");
                    break;
            }
        }

        private EventGridPubClient CreateEventGridPubClient(string clientType)
        {
            string? TopicEndpoint = null;
            string? TopicKey = null;

            if (clientType.ToLower().Equals("request"))
            {
                TopicEndpoint = _configuration["RequestEventGridTopicEndpoint"];
                TopicKey = _configuration["RequestEventGridTopicKey"];
            }
            else if (clientType.ToLower().Equals("result"))
            {
                TopicEndpoint = _configuration["ResponseEventGridTopicEndpoint"];
                TopicKey = _configuration["ResponseEventGridTopicKey"];
            }
            else
            {
                throw new ArgumentException("Invalid client type");
            }

            if (string.IsNullOrWhiteSpace(TopicEndpoint) || string.IsNullOrWhiteSpace(TopicKey))
            {
                throw new ArgumentException(clientType, " Event Grid Topic Endpoint and Key must be provided.");
            }

            return new EventGridPubClient(TopicEndpoint, TopicKey);
        }

        private EventType GetEventType(IReadOnlyDictionary<string, object?>? eventData)
        {
            _logger.LogInformation("GetEventType called"); 
            var eventProperties = (eventData["PropertiesArray"]).ToString();

            _logger.LogInformation($"eventProperties: {eventProperties}");

            if (eventProperties.Contains("UserRequestEvent"))
            {
                return EventType.UserRequestEvent;
            }
            else if(eventProperties.Contains("ProcessingCompleteEvent"))
            {
                return EventType.ProcessingCompleteEvent;
            }
            else
            {
                _logger.LogInformation("Unexpected Event");

                var splitEventType = eventProperties.Split("EventType");
                _logger.LogInformation($"splitEventType: {splitEventType}");

                var splitComma = splitEventType[1].Split(",");
                _logger.LogInformation($"splitComma: {splitComma}");

                var eventType = splitComma[0].Remove(0, 2);
                _logger.LogInformation($"Unexpected event type: {eventType}");
                _logger.LogError($"Unexpected event type: {eventType}");
                
                return EventType.UnexpectedEventType;
            }
        }

        private async Task<bool> ProcessUserRequestEvent(string eventAsString)
        {
            UserRequest? userRequest = DeserializeUserRequest(eventAsString);
            if (userRequest is null)
            {
                return false;
            }

            if (!IsValidTransactionType(userRequest))
            {
                return false;
            }

            var sendResult = await _reqEventGridClient.PublishEventGridEvent(userRequest.RequestId, "UserRequestEvent", userRequest);
            if (sendResult == 200)
            {
                _logger.LogInformation("UserRequestEvent published successfully");
                return true;
            }
            else
            {
                _logger.LogError("Failed to publish UserRequestEvent");
                return false;
            }
        }

        private UserRequest? DeserializeUserRequest(string input)
        {
            UserRequest? userRequest = null;
            try
            {
                userRequest = JsonSerializer.Deserialize<UserRequest>(input);
                if (userRequest is null)
                {
                    _logger.LogError("Deserialized user request is null");
                }
            }
            catch
            {
                _logger.LogError("Failed to deserialize user request");
            }
            return userRequest;
        }

        private bool IsValidTransactionType(UserRequest userRequest)
        {
            bool IsValid = true;

            CommonModels.Transaction transaction = userRequest.UserTransaction;

            switch (transaction.TransactionType.ToLower())
            {
                case "buy":
                    _logger.LogInformation($"Buying {transaction.Quantity} shares of {transaction.StockName} for {userRequest.UserName}");
                    break;
                case "sell":
                    _logger.LogInformation($"Selling {transaction.Quantity} shares of {transaction.StockName} for {userRequest.UserName}");
                    break;
                default:
                    _logger.LogError($"Invalid transaction type: {transaction.TransactionType}");
                    IsValid = false;
                    break;
            }

            return IsValid;
        }

        private async Task<bool> ProcessProcessingCompleteEvent(string eventAsString)
        {
            ClientNotification? clientNotification = DeserialiseProcessingCompleteEvent(eventAsString);
            if (clientNotification is null)
            {
                return false;
            }

            var sendResult = await _resEventGridClient.PublishEventGridEvent(clientNotification.RequestId, "ClientNotification", clientNotification);
            if (sendResult == 200)
            {
                _logger.LogInformation("ClientNotification published successfully");
                return true;
            }
            else
            {
                _logger.LogError("Failed to publish ClientNotification");
                return false;
            }
        }

        private ClientNotification? DeserialiseProcessingCompleteEvent(string input)
        {
            ClientNotification? clientNotification = null;
            try
            {
                clientNotification = JsonSerializer.Deserialize<ClientNotification>(input);
                if (clientNotification is null)
                {
                    _logger.LogError("Deserialized client notification is null");
                }
            }
            catch
            {
                _logger.LogError("Failed to deserialize client notification");
            }
            return clientNotification;
        }

    }
}
