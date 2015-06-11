using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using JetBlack.MessageBus.Common.Network;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;
using log4net;
using Newtonsoft.Json.Linq;

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

            new IPEndPoint(IPAddress.Loopback, 9090)
                .ToConnectObservable()
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(
                    tcpClient =>
                    {
                        var client = new Client<JObject>(tcpClient, new JsonEncoder<JObject>(), maxBufferPoolSize, maxBufferSize, TaskPoolScheduler.Default, cts.Token);
                        client.OnDataReceived += OnDataReceived;
                        client.AddSubscription("LSE.VOD");
                        client.AddSubscription("LSE.TSCO");
                        client.AddSubscription("LSE.FOOBAR");
                    });

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
