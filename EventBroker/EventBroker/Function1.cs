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
            var eventType = GetEventType(eventData);
            _logger.LogInformation($"eventType = {eventType}");

            if (eventType.Equals("UserRequestEvent"))
            {
                _logger.LogInformation("Processing UserRequestEvent");
                await ProcessUserRequestEvent(input[0]);
            }
            else if (eventType.Equals("ProcessingCompleteEvent"))
            {
                _logger.LogInformation("Processing ProcessingCompleteEvent");
                await ProcessProcessingCompleteEvent(input[0]);
            }
            else
            {
                _logger.LogError($"Unexpected event type {eventType}");
            }
 
            //switch (eventType)
            //{
            //    case "UserRequestEvent":
            //        _logger.LogInformation("Processing UserRequestEvent");
            //        await ProcessUserRequestEvent(input[0]);
            //        break;
            //    case "ProcessingCompleteEvent":
            //        _logger.LogInformation("Processing ProcessingCompleteEvent");
            //        await ProcessProcessingCompleteEvent(input[0]);
            //        break;
            //    default:
            //        _logger.LogError($"Unexpected event type {eventType}");
            //        break;
            //}

            //if (input[0].Contains("TransactionType"))
            //{
            //    var resultCode = await ProcessUserRequestEvent(input[0]);
            //    _logger.LogInformation($"ProcessUserRequestEvent returned: {resultCode}");
            //}
            //else if (input[0].Contains("ProcessingSuccessful"))
            //{
            //    var resultCode = await ProcessProcessingCompleteEvent(input[0]);
            //    _logger.LogInformation($"ProcessProcessingCompleteEvent returned: {resultCode}");
            //}
            //else
            //{
            //    _logger.LogError("Unexpected event type");
            //}
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

        private string GetEventType(IReadOnlyDictionary<string, object?>? eventData)
        {
            // There will be a more elegant way ...
            var eventProperties = eventData["PropertiesArray"];
            var splitEventType = eventProperties.ToString().Split("EventType");

            var splitComma = splitEventType[1].Split(",");
            var eventType = splitComma[0].Remove(0, 2);

            return eventType.Trim();
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
