using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using Newtonsoft.Json.Linq;
using JetBlack.MessageBus.Json;
using JetBlack.MessageBus.TopicBus.Adapters;

namespace JetBlack.MessageBus.Examples.TopicBusCachingPublisher
{
    internal class Program
    {
        private const int PublishMs = 100;

        private static readonly Random Rnd = new Random();

        private static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            try
            {
                MainAsync(args).Wait();
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed: " + error.Message);
            }
        }

        static async Task MainAsync(string[] args)
        {
            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;
            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);

            var endpoint = new IPEndPoint(IPAddress.Loopback, 9090);

            var client = await Client.Create(endpoint, new JsonEncoder<JObject>(), bufferManager, TaskPoolScheduler.Default);
            var cachingPublisher = new CachingPublisher<JObject, string, JToken>(client);

            var cts = new CancellationTokenSource();
            StartPublishing(cachingPublisher, TaskPoolScheduler.Default, cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            cts.Cancel();
            client.Dispose();
        }

        private static void StartPublishing(CachingPublisher<JObject, string, JToken> cachingPublisher, IScheduler publishScheduler, CancellationToken token)
        {
            // Prepare some data.
            var marketData = new Dictionary<string, JObject>
            {
                {
                    "LSE.VOD", new JObject
                    {
                        {"NAME", "Vodafone Group PLC"},
                        {"BID", 140.60},
                        {"ASK", 140.65}
                    }
                },
                {
                    "LSE.TSCO", new JObject
                    {
                        {"NAME", "Tesco PLC"},
                        {"BID", 423.15},
                        {"ASK", 423.25}
                    }
                },
                {
                    "LSE.SBRY", new JObject
                    {
                        {"NAME", "J Sainsbury PLC"},
                        {"BID", 325.30},
                        {"ASK", 325.35}
                    }
                }
            };

            // Request a notification when the the following regex pattern is
            // requested. This corresponds to the things we are publishing.
            cachingPublisher.AddNotification(@"LSE\..*");

            // Publish the data.
            foreach (var item in marketData)
                cachingPublisher.Publish(item.Key, item.Value);

            foreach (var item in marketData)
                ScheduleUpdate(cachingPublisher, item.Key, item.Value, publishScheduler, token);
        }

        private static void ScheduleUpdate(CachingPublisher<JObject,string,JToken> cachingPublisher, string topic, JObject data, IScheduler scheduler, CancellationToken token)
        {
            scheduler.Schedule(
                TimeSpan.FromMilliseconds(PublishMs * Rnd.Next(5, 100)),
                () =>
                {
                    if (!token.IsCancellationRequested)
                    {
                        PublishUpdate(cachingPublisher, topic, data);
                        ScheduleUpdate(cachingPublisher, topic, data, scheduler, token);
                    }
                });
        }

        private static void PublishUpdate(CachingPublisher<JObject,string,JToken> cachingPublisher, string topic, JObject data)
        {
            // Pevert the data a little.
            var bid = (double) data["BID"];
            var ask = (double) data["ASK"];
            var spread = ask - bid;
            var newData = new JObject
            {
                {"BID",  Math.Round(bid + bid * Rnd.NextDouble() * 5.0 / 100.0, 2)},
                {"ASK",  Math.Round(bid + spread, 2)}
            };
            data["BID"] = Math.Round(bid + bid * Rnd.NextDouble() * 5.0 / 100.0, 2);
            data["ASK"] = Math.Round(bid + spread, 2);
            Console.WriteLine("{0}: {1}", topic, newData);
            cachingPublisher.Publish(topic, newData);
        }
    }
}
