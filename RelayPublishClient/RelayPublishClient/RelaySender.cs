using CommonModels;
using Microsoft.Azure.Relay;

namespace RelayPublishClient
{
    public class RelaySender
    {
        private readonly HybridConnectionClient _hybridConnectionClient;

        public RelaySender(RelayConfiguration configuration)
        {
            var relayNamespace = configuration.RelayNamespace;
            var connectionName = configuration.ConnectionName;
            var keyName = configuration.KeyName;
            var key = configuration.Key;

            if(string.IsNullOrEmpty(relayNamespace) 
                || string.IsNullOrEmpty(connectionName) 
                || string.IsNullOrEmpty(keyName) 
                || string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Invalid configuration: one or more parameters are not specified");
            }

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(keyName, key);
            _hybridConnectionClient = new HybridConnectionClient(new Uri(String.Format("sb://{0}/{1}", relayNamespace, connectionName)), tokenProvider);
        }

        public async Task<(bool, string)> Send(List<string> stringsToSend)
        {
            bool sent = true;
            string errMsg = string.Empty;

            var relayConnection = await _hybridConnectionClient.CreateConnectionAsync();
            var writer = new StreamWriter(relayConnection) { AutoFlush = true };
            
            try
            {
                foreach (string oneString in stringsToSend)
                {
                    await writer.WriteLineAsync(oneString);
                }
            }
            catch(Exception ex)
            {
                sent = false;
                errMsg = $"Error sending message: {ex.Message}";
            }
            finally
            {
                await writer.FlushAsync();
                writer.Close();
            }
            return (sent, errMsg);
        }
    }
}
