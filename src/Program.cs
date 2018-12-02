using CommandLine;
using RokuDotNet.Client;
using RokuDotNet.Proxy;
using RokuDotNet.Proxy.IoTHub;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RokuDotNet.Relay
{
    class Program
    {
        [Verb("list", HelpText = "List Roku devices on the local network.")]
        internal sealed class ListOptions
        {
        }

        [Verb("listen", HelpText = "Listen for remote requests of a Roku device.")]
        internal sealed class ListenOptions
        {
            [Option('s', "serialNumber", HelpText = "The serial number of the Roku device.")]
            public string SerialNumber { get; set; }

            [Option('c', "connectionString", HelpText = "The Azure IoT Hub connection string for the Roku device.")]
            public string ConnectionString { get; set; }
        }

        public static Task Main(string[] args)
        {
            return Parser
                .Default
                .ParseArguments<ListOptions, ListenOptions>(args)
                .MapResult<ListOptions, ListenOptions, Task>(
                    ListDevicesAsync,
                    ListenAsync,
                    errs => Task.FromException(new InvalidOperationException(""))
                );
        }

        private static async Task ListDevicesAsync(ListOptions options)
        {
            var discoveryClient = new UdpRokuDeviceDiscoveryClient();

            Func<HttpDiscoveredDeviceContext, Task<bool>> func =
                async (HttpDiscoveredDeviceContext context) =>
                {
                    Console.WriteLine($"Found device: {context.SerialNumber}");

                    var details = await context.Device.Query.GetDeviceInfoAsync();

                    return false;
                };

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                var discoverTask = discoveryClient.DiscoverDevicesAsync(func, cancellationTokenSource.Token);

                Console.WriteLine("Searching for Roku devices...");
                Console.WriteLine("Press <ENTER> to quit.");
                
                Console.ReadLine();

                cancellationTokenSource.Cancel();

                try
                {
                    await discoverTask;
                }
                catch (OperationCanceledException)
                {
                    // No-op.
                }
            }
        }

        private static async Task ListenAsync(ListenOptions options)
        {
            using (var cts = new CancellationTokenSource())
            {
                Func<string, CancellationToken, Task<IRokuDevice>> deviceMapFunc =
                    (_, __) =>
                    {
                        var discoveryClient = new UdpRokuDeviceDiscoveryClient();

                        return discoveryClient.DiscoverFirstDeviceAsync();
                    };

                var handler = new RokuRpcServerHandler(deviceMapFunc);
                var server = new IoTHubRokuRpcServer(options.ConnectionString, handler);

                await server.StartListeningAsync(cts.Token);

                try
                {
                    Console.WriteLine("Listening for device commands (press <ENTER> to quit)...");

                    Console.ReadLine();
                }
                finally
                {
                    await server.StopListeningAsync(cts.Token);
                }
            }
        }
    }
}
