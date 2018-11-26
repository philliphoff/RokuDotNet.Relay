using RokuDotNet.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RokuDotNet.Relay
{
    class Program
    {
        static async Task Main(string[] args)
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

                Console.WriteLine("Press 'q' to quit...");
                
                while (true)
                {
                    var key = (char)Console.Read();

                    if (key == 'q')
                    {
                        cancellationTokenSource.Cancel();

                        break;
                    }
                }

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
