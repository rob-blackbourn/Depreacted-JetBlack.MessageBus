using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.Network;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;
using Newtonsoft.Json.Linq;

namespace JetBlack.MessageBus.Examples.TopicBusPublisher
{
    class Program
    {
        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            new IPEndPoint(IPAddress.Loopback, 9090)
                .ToConnectObservable()
                .Subscribe(tcpClient =>
                {
                    var cts = new CancellationTokenSource();
                    var client = new Client<JObject>(tcpClient, new JsonEncoder<JObject>(), TaskPoolScheduler.Default, cts.Token);
                    client.Publish(
                        "LSE.VOD",
                        true,
                        new JObject
                        {
                            {"NAME", "Vodafone Group PLC"},
                            {"BID", 140.60},
                            {"ASK", 140.65}
                        });
                });

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();
        }
    }
}
