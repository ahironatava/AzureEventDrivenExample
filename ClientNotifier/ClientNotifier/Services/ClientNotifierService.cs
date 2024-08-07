using ClientNotifier.Interfaces;
using CommonModels;
using RelayPublishClient;

namespace ClientNotifier.Services
{
    public class ClientNotifierService : IClientNotifierService
    {
        private readonly RelaySender _relaySender;

        private readonly ILogger<ClientNotifierService> _logger;

        public ClientNotifierService(IConfiguration configuration, ILogger<ClientNotifierService> logger)
        {
            _logger = logger;

            RelayConfiguration relayConfiguration = new RelayConfiguration()
            {
                RelayNamespace = configuration["relayNamespace"],
                ConnectionName = configuration["connectionName"],
                KeyName = configuration["keyName"],
                Key = configuration["key"]
            };

            _relaySender = new RelaySender(relayConfiguration);
        }

        public async Task<bool> NotifyClient(GridEvent<dynamic> gridEvent)
        {
            // Notify the client, by passing the relevant information through the Azure Relay
            List<string> stringList = new List<string>();
            stringList.Add($"RequestId {gridEvent.Data.RequestId}");
            stringList.Add($"Successful = {gridEvent.Data.ProcessingSuccessful}");

            _logger.LogInformation($"Notify with: RequestId = {gridEvent.Data.RequestId}, Successful = {gridEvent.Data.ProcessingSuccessful}");

            (bool sent, string errMsg) = await _relaySender.Send(stringList);
            if(! sent)
            {
                _logger.LogError(errMsg);
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
