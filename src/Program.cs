using CommandLine;
using RokuDotNet.Client;
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

        public static Task Main(string[] args)
        {
            return Parser
                .Default
                .ParseArguments<ListOptions>(args)
                .MapResult(
                    ListDevicesAsync,
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
    }
}
