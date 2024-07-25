using ClientNotifier.Interfaces;
using CommonModels;
using RelayPublishClient;

namespace ClientNotifier.Services
{
    public class ClientNotifierService : IClientNotifierService
    {
        private readonly RelaySender _relaySender;

        public ClientNotifierService(IConfiguration configuration)
        {
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
            stringList.Add(gridEvent.Data["RequestId"]);
            stringList.Add(gridEvent.Data["ProcessingSuccessful"]);
            await _relaySender.Send(stringList);

            return true;
        }
    }
}
