using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace JetBlack.MessageBus.Examples.TopicBusPublisher
{
    class Program
    {
        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;

            var cts = new CancellationTokenSource();
            var endpoint = new IPEndPoint(IPAddress.Loopback, 9090);

            Task.Run(async () => await Publish(endpoint, maxBufferPoolSize, maxBufferSize, cts.Token), cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            cts.Cancel();
        }

        private static async Task Publish(IPEndPoint endpoint, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);

            var client = new TypedClient<JObject>(tcpClient, new JsonEncoder<JObject>(), maxBufferPoolSize, maxBufferSize, TaskPoolScheduler.Default, token);
            client.Publish(
                "LSE.VOD",
                true,
                new JObject
                {
                            {"NAME", "Vodafone Group PLC"},
                            {"BID", 140.60},
                            {"ASK", 140.65}
                });
        }
    }
}
