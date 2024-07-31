using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;

namespace AzureRelayListener
{
    // Based on https://learn.microsoft.com/en-us/azure/azure-relay/relay-hybrid-connections-dotnet-get-started

    public class Program
    {
        private static string? _relayNamespace = string.Empty;
        private static string? _connectionName = string.Empty;
        private static string? _keyName = string.Empty;
        private static string? _key = string.Empty;

        public static void Main(string[] args)
        {
            var buidler = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.json", true, true);
            var configuration = buidler.Build();

            var relaySettings = configuration.GetSection("RelaySettings");
            _relayNamespace = relaySettings["RelayNamespace"];
            _connectionName = relaySettings["ConnectionName"];
            _keyName = relaySettings["KeyName"];
            _key = relaySettings["Key"];

            RunAsync().GetAwaiter().GetResult();
        }

        private static async Task RunAsync()
        {
            var cts = new CancellationTokenSource();

            var tokenProvider = TokenProvider.CreateSharedAccessSignatureTokenProvider(_keyName, _key);
            var listener = new HybridConnectionListener(new Uri(string.Format("sb://{0}/{1}", _relayNamespace, _connectionName)), tokenProvider);

            listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
            listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
            listener.Online += (o, e) => { Console.WriteLine("Online"); };

            // Opening the listener establishes the control channel to
            // the Azure Relay service. The control channel is continuously 
            // maintained, and is reestablished when connectivity is disrupted.
            await listener.OpenAsync(cts.Token);
            Console.WriteLine("Server listening");

            // Provide callback for a cancellation token that will close the listener.
            cts.Token.Register(() => listener.CloseAsync(CancellationToken.None));

            // Start a new thread that will continuously read the console.
            new Task(() => Console.In.ReadLineAsync().ContinueWith((s) => { cts.Cancel(); })).Start();

            // Accept the next available, pending connection request. 
            // Shutting down the listener allows a clean exit. 
            // This method returns null.
            while (true)
            {
                var relayConnection = await listener.AcceptConnectionAsync();
                if (relayConnection == null)
                {
                    break;
                }

                ProcessMessagesOnConnection(relayConnection, cts);
            }

            // Close the listener after you exit the processing loop.
            await listener.CloseAsync(cts.Token);
        }

        private static async void ProcessMessagesOnConnection(HybridConnectionStream relayConnection, CancellationTokenSource cts)
        {
            Console.WriteLine("New session");

            // The connection is a fully bidrectional stream. 
            // Put a stream reader and a stream writer over it.  
            // This allows you to read UTF-8 text that comes from 
            // the sender, and to write text replies back.
            var reader = new StreamReader(relayConnection);
            var writer = new StreamWriter(relayConnection) { AutoFlush = true };
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    // Read a line of input until a newline is encountered.
                    var line = await reader.ReadLineAsync();

                    if (string.IsNullOrEmpty(line))
                    {
                        // If there's no input data, signal that 
                        // you will no longer send data on this connection.
                        // Then, break out of the processing loop.
                        await relayConnection.ShutdownAsync(cts.Token);
                        break;
                    }

                    // Write the line on the console.
                    Console.WriteLine(line);

                    // Write the line back to the client, prepended with "Echo:"
                    await writer.WriteLineAsync($"Echo: {line}");
                }
                catch (IOException)
                {
                    // Catch an I/O exception. This likely occurred when
                    // the client disconnected.
                    Console.WriteLine("Client closed connection");
                    break;
                }
            }

            Console.WriteLine("End session");

            await relayConnection.CloseAsync(cts.Token);
        }
    }
}