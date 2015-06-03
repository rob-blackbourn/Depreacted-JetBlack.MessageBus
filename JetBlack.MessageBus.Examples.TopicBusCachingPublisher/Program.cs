using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using JetBlack.MessageBus.Common.Network;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Reactive.Concurrency;
using System.Net.Sockets;

namespace JetBlack.MessageBus.Examples.TopicBusCachingPublisher
{
    class Program
    {
        class MainClass
        {
            private static readonly Random rnd = new Random();

            static void Main(string[] args)
            {
                log4net.Config.XmlConfigurator.Configure();

                log4net.Config.XmlConfigurator.Configure();

                var cts = new CancellationTokenSource();

                new IPEndPoint(IPAddress.Loopback, 9090)
                    .ToConnectObservable()
                    .Subscribe(socket =>
                    {
                        CreatePublisher(socket, TaskPoolScheduler.Default, cts.Token);
                    });

                Console.WriteLine("Press <ENTER> to quit");
                Console.ReadLine();

                cts.Cancel();
            }

            static void CreatePublisher(Socket socket, IScheduler publishScheduler, CancellationToken token)
            {
                var client = new Client<JObject>(socket, new JsonEncoder<JObject>(), TaskPoolScheduler.Default, token);
                var cachingPublisher = new CachingPublisher<JObject, JToken>(client);

                // Prepare the market data.
                var marketData = new Dictionary<string, JObject>()
                        {
                            { "LSE.VOD", new JObject
                                {
                                    { "NAME", "Vodafone Group PLC" },
                                    { "BID", 140.60 },
                                    { "ASK", 140.65 }
                                }
                            },
                            { "LSE.TSCO", new JObject
                                {
                                    { "NAME", "Tesco PLC" },
                                    { "BID", 423.15 },
                                    { "ASK", 423.25 }
                                }
                            },
                            { "LSE.SBRY", new JObject
                                {
                                    { "NAME", "J Sainsbury PLC" },
                                    { "BID", 325.30 },
                                    { "ASK", 325.35 }
                                }
                            }
                        };

                // Request a notification when the the following regex pattern is
                // requested. This corresponds to the things we are publishing.
                client.AddNotification(@"LSE\..*");

                // Publish the data.
                foreach (var item in marketData)
                {
                    cachingPublisher.Publish(item.Key, item.Value);
                    // Remove the name so we don't republish it.
                    item.Value.Remove("NAME");
                }

                foreach (var item in marketData)
                    ScheduleUpdate(cachingPublisher, item.Key, item.Value, publishScheduler, token);
            }

            static void ScheduleUpdate(
                CachingPublisher<JObject, JToken> cachingPublisher,
                string topic,
                JObject data,
                IScheduler scheduler,
                CancellationToken token)
            {
                scheduler.Schedule(
                    TimeSpan.FromMilliseconds(10 * rnd.Next(5, 100)),
                    () =>
                    {
                        if (!token.IsCancellationRequested)
                        {
                            PublishUpdate(cachingPublisher, topic, data);
                            ScheduleUpdate(cachingPublisher, topic, data, scheduler, token);
                        }
                    });
            }

            static void PublishUpdate(
                CachingPublisher<JObject, JToken> cachingPublisher,
                string topic,
                JObject data)
            {
                // Pevert the data a little.
                var bid = (double)data["BID"];
                var ask = (double)data["ASK"];
                var spread = ask - bid;
                data["BID"] = Math.Round(bid + bid * rnd.NextDouble() * 5.0 / 100.0, 2);
                data["ASK"] = Math.Round(bid + spread, 2);
                Console.WriteLine("{0}, BID={1}, ASK={2}", topic, data["BID"], data["ASK"]);
                cachingPublisher.Publish(topic, data);
            }
        }
    }
}
