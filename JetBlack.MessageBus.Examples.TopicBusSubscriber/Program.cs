using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using log4net;
using Newtonsoft.Json.Linq;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;

namespace JetBlack.MessageBus.Examples.TopicBusSubscriber
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
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
            var client = await Client.Create(endpoint, new JsonEncoder<JObject>(), bufferManager, TaskPoolScheduler.Default);

            client.OnDataReceived += OnDataReceived;
            client.AddSubscription("LSE.VOD");
            client.AddSubscription("LSE.TSCO");
            client.AddSubscription("LSE.FOOBAR");

            // Wait to exit.
            Console.WriteLine("Press <enter> to quit");
            Console.ReadLine();

            // Tidy up.
            client.Dispose();
        }

        private static void OnDataReceived(object sender, DataReceivedEventArgs<JObject> e)
        {
            Console.WriteLine("OnData: {0} {1}", e.Topic, e.IsImage);
            if (e.Data == null)
                Console.WriteLine("No Data");
            else
                Console.WriteLine("Data: {0}", e.Data);
        }
    }
}
