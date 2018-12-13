using CommandLine;
using RokuDotNet.Client;
using RokuDotNet.Proxy;
using RokuDotNet.Proxy.IoTHub;
using Serilog;
using System;
using System.Net.Http;
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

        private static ILogger Logger =
            new LoggerConfiguration()
                .WriteTo
                .Console()
                .CreateLogger();

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
                    Logger.Information("Found device {SerialNumber} at {Location}", context.SerialNumber, context.Device.Location);

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
            using (var httpHandler = new HttpClientHandler())
            using (var consoleLoggingHandler = new HttpLoggingHandler(httpHandler, Logger))
            using (var deviceLock = new SemaphoreSlim(1, 1))
            {
                IRokuDevice device = null;

                Func<string, CancellationToken, Task<IRokuDevice>> deviceMapFunc =
                    async (string deviceId, CancellationToken cancellationToken) =>
                    {
                        // NOTE: IoTHubRokuRpcServer allows only a single device per server,
                        //       so there's technically no mapping to perform.

                        if (device == null)
                        {
                            await deviceLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                            try
                            {
                                if (device == null)
                                {
                                    var discoveryClient = new UdpRokuDeviceDiscoveryClient(consoleLoggingHandler);

                                    device = await discoveryClient.DiscoverSpecificDeviceAsync(options.SerialNumber, cancellationToken).ConfigureAwait(false);
                                }
                            }
                            finally
                            {
                                deviceLock.Release();
                            }
                        }

                        return device;
                    };

                var server =
                    new IoTHubRokuRpcServer(
                        options.ConnectionString,
                        new RpcLoggingHandler(
                            new RokuRpcServerHandler(deviceMapFunc),
                            Logger));

                await server.StartListeningAsync(cts.Token);

                try
                {
                    Console.WriteLine("Listening for device \"{0}\" commands (press <ENTER> to quit)...", options.SerialNumber);

                    Console.ReadLine();
                }
                finally
                {
                    await server.StopListeningAsync(cts.Token);

                    if (device != null)
                    {
                        device.Dispose();
                    }
                }
            }
        }
    }
}
