using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using Newtonsoft.Json.Linq;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;
using log4net;

namespace JetBlack.MessageBus.Examples.TopicBusPublisher
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception error)
            {
                Log.Error("Failed", error);
            }
        }

        static async Task MainAsync(string[] args)
        {
            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;
            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);

            var endpoint = new IPEndPoint(IPAddress.Loopback, 9090);

            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);
            var client = await Client.Create(endpoint, new JsonEncoder<JObject>(), bufferManager, TaskPoolScheduler.Default);

            client.Publish(
                "LSE.VOD",
                true,
                new JObject
                {
                    {"NAME", "Vodafone Group PLC"},
                    {"BID", 140.60},
                    {"ASK", 140.65}
                });

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            client.Dispose();
        }
    }
}
