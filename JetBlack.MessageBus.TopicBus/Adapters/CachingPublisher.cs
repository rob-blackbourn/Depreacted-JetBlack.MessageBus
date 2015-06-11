using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class CachingPublisher<T, TValue> : Client<T> where T:IDictionary<string,TValue>
    {
        private readonly Cache _cache;
        private readonly object _gate = new Object();

        public CachingPublisher(Socket socket, IByteEncoder<T> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(socket, byteEncoder, maxBufferPoolSize, maxBufferSize, scheduler, token)
        {
            _cache = new Cache(this);
            OnForwardedSubscription += (sender, args) =>
            {
                lock (_gate)
                {
                    if (args.IsAdd)
                        _cache.AddSubscription(args.ClientId, args.Topic);
                    else
                        _cache.RemoveSubscription(args.ClientId, args.Topic);
                }
            };
        }

        public void Publish(string topic, T data)
        {
            lock (_gate)
            {
                _cache.Publish(topic, data);
            }
        }

        class Cache : Dictionary<string, CacheItem>
        {
            private readonly Client<T> _client;

            public Cache(Client<T> client)
            {
                _client = client;
            }

            public void AddSubscription(int clientId, string topic)
            {
                // Have we received a subscription or published data on this topic yet?
                CacheItem cacheItem;
                if (!TryGetValue(topic, out cacheItem))
                    Add(topic, cacheItem = new CacheItem());

                // Has this client already subscribed to this topic?
                if (!cacheItem.ClientStates.ContainsKey(clientId))
                {
                    // Add the client to the cache item, and indicate that we have not yet sent an image.
                    cacheItem.ClientStates.Add(clientId, false);
                }

                if (!cacheItem.ClientStates[clientId] && !Equals(cacheItem.Data, default(T)))
                {
                    // Send the image and mark this client appropriately.
                    cacheItem.ClientStates[clientId] = true;

                    _client.Send(clientId, topic, true, cacheItem.Data);
                }
            }

            public void RemoveSubscription(int clientId, string topic)
            {
                // Have we received a subscription or published data on this topic yet?
                CacheItem cacheItem;
                if (!TryGetValue(topic, out cacheItem))
                    return;

                // Does this topic have this client?
                if (!cacheItem.ClientStates.ContainsKey(clientId))
                    return;

                cacheItem.ClientStates.Remove(clientId);

                // If there are no clients and no data remove the item.
                if (cacheItem.ClientStates.Count == 0 && Equals(cacheItem.Data, default(T)))
                    Remove(topic);
            }

            public void Publish(string topic, T data)
            {
                // If the topic is not in the cache add it.
                CacheItem cacheItem;
                if (!TryGetValue(topic, out cacheItem))
                    Add(topic, cacheItem = new CacheItem { Data = data });

                // Bring the cache data up to date.
                if (Equals(data, default(T)) || Equals(cacheItem.Data, default(T)))
                    cacheItem.Data = data; // overwrite
                else // update
                    foreach (var item in data)
                        cacheItem.Data[item.Key] = item.Value;

                foreach (var clientState in cacheItem.ClientStates.ToList())
                {
                    if (clientState.Value)
                        _client.Send(clientState.Key, topic, false, data);
                    else
                    {
                        // Deliver idividual messages to any clients yet to receive an image.
                        _client.Send(clientState.Key, topic, true, cacheItem.Data);
                        cacheItem.ClientStates[clientState.Key] = true;
                    }
                }
            }
        }

        class CacheItem
        {
            // Remember whether this client id has already received the image.
            public readonly Dictionary<int, bool> ClientStates = new Dictionary<int, bool>();
            // The cache of data constituting the image.
            public T Data;
        }
    }
}
