using System;
using System.Collections.Generic;
using System.Linq;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class CachingPublisher<T, TValue> where T:IDictionary<string,TValue>
    {
        public event EventHandler<DataReceivedEventArgs<T>> OnDataReceived;

        readonly Dictionary<string, CacheItem> _topicCache = new Dictionary<string, CacheItem>();

        public CachingPublisher(Client<T> client)
        {
            Client = client;
            Client.OnForwardedSubscription += OnForwardedSubscription;
        }

        public Client<T> Client { get; private set; }

        private void OnForwardedSubscription(object sender, ForwardedSubscriptionEventArgs e)
        {
            if (e.IsAdd)
                Add(e.ClientId, e.Topic);
            else
                Remove(e.ClientId, e.Topic);
        }

        private void Add(int clientId, string topic)
        {
            lock (_topicCache)
            {
                // Have we received a subscription or published data on this topic yet?
                CacheItem cacheItem;
                if (!_topicCache.TryGetValue(topic, out cacheItem))
                    _topicCache.Add(topic, cacheItem = new CacheItem());

                // Has this client already subscribed to this topic?
                if (!cacheItem.ClientStates.ContainsKey(clientId))
                {
                    // Add the client to the cache item, and indicate that we have not yet sent an image.
                    cacheItem.ClientStates.Add(clientId, false);
                }

                if (!cacheItem.ClientStates[clientId] && cacheItem.Data != null)
                {
                    // Send the image and mark this client appropriately.
                    cacheItem.ClientStates[clientId] = true;

                    Client.Send(clientId, topic, true, cacheItem.Data);
                }
            }
        }

        private void Remove(int clientId, string topic)
        {
            lock (_topicCache)
            {
                // Have we received a subscription or published data on this topic yet?
                CacheItem cacheItem;
                if (_topicCache.TryGetValue(topic, out cacheItem))
                {
                    // Does this topic have this client?
                    if (cacheItem.ClientStates.ContainsKey(clientId))
                    {
                        cacheItem.ClientStates.Remove(clientId);

                        // If there are no clients and no data remove the item.
                        if (cacheItem.ClientStates.Count == 0 && cacheItem.Data == null)
                            _topicCache.Remove(topic);
                    }
                }
            }
        }

        public void Publish(string topic, T data)
        {
            Publish(topic, false, data);
        }

        void Publish(string topic, bool isImage, T data)
        {
            // If the topic is not in the cache add it.
            CacheItem cacheItem;
            if (!_topicCache.TryGetValue(topic, out cacheItem))
                _topicCache.Add(topic, cacheItem = new CacheItem { Data = data });

            // Bring the cache data up to date.
            if (data != null)
                foreach (var item in data)
                    cacheItem.Data[item.Key] = item.Value;

            // Are there any clients yet to receive any message on this feed/topic?
            if (cacheItem.ClientStates.Values.Any(c => !c))
            {
                // Yes. Deliver idividual messages.
                foreach (var clientId in cacheItem.ClientStates.Keys.ToArray())
                {
                    if (cacheItem.ClientStates[clientId])
                        Client.Send(clientId, topic, isImage, data);
                    else
                    {
                        Client.Send(clientId, topic, true, cacheItem.Data);
                        cacheItem.ClientStates[clientId] = true;
                    }
                }
            }
            else
                Client.Publish(topic, isImage, data);
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
