using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
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

            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;

            var cts = new CancellationTokenSource();

            var endpoint = new IPEndPoint(IPAddress.Loopback, 9090);

            //var client = Task.Run(async () => await Start(endpoint, maxBufferPoolSize, maxBufferSize, cts.Token), cts.Token).Result;
            //var client = Start(endpoint, maxBufferPoolSize, maxBufferSize, cts.Token).Result;
            Log.Debug("Creating a client");
            var client = TypedClient<JObject>.Create(endpoint, new JsonEncoder<JObject>(), maxBufferPoolSize, maxBufferSize, TaskPoolScheduler.Default, cts.Token).Result;
            Log.Debug("Got a client");

            client.OnDataReceived += OnDataReceived;
            client.AddSubscription("LSE.VOD");
            client.AddSubscription("LSE.TSCO");
            client.AddSubscription("LSE.FOOBAR");

            // Wait to exit.
            Console.WriteLine("Press <enter> to quit");
            Console.ReadLine();

            // Tidy up.
            cts.Cancel();
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
