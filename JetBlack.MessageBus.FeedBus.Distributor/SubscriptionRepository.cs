using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class SubscriptionRepository
    {
        // Topic->Interactor->SubscriptionCount.
        private readonly IDictionary<string, IDictionary<string, CountedSet<IInteractor>>> _cache = new Dictionary<string, IDictionary<string, CountedSet<IInteractor>>>();

        public void AddSubscription(IInteractor subscriber, string feed, string topic)
        {
            // Find the subscribers to topics for this feed.
            IDictionary<string, CountedSet<IInteractor>> subscribersForFeed;
            if (!_cache.TryGetValue(feed, out subscribersForFeed))
                _cache.Add(feed, subscribersForFeed = new Dictionary<string, CountedSet<IInteractor>>());

            // Find the list of interactors that have subscribed to this topic.
            CountedSet<IInteractor> subscribersForTopic;
            if (!subscribersForFeed.TryGetValue(topic, out subscribersForTopic))
                subscribersForFeed.Add(topic, new CountedSet<IInteractor>(new[] { subscriber }));
            else
                subscribersForTopic.Increment(subscriber);
        }

        public void RemoveSubscription(IInteractor subscriber, string feed, string topic, bool removeAll)
        {
            // Find the subscribers to topics for this feed.
            IDictionary<string, CountedSet<IInteractor>> subscribersForFeed;
            if (!_cache.TryGetValue(feed, out subscribersForFeed))
                return;

            // Can we find this topic in the cache?
            CountedSet<IInteractor> subscribersForTopic;
            if (!subscribersForFeed.TryGetValue(topic, out subscribersForTopic))
                return;

            // Has this subscriber registered an interest in the topic?
            if (!subscribersForTopic.Contains(subscriber))
                return;

            if (removeAll)
                subscribersForTopic.Delete(subscriber);
            else
                subscribersForTopic.Decrement(subscriber);

            // If there are no subscribers left on this topic, remove it from the cache.
            if (subscribersForTopic.Count == 0)
                subscribersForFeed.Remove(topic);

            if (subscribersForFeed.Count == 0)
                _cache.Remove(feed);
        }

        // TODO: Rename
        public IEnumerable<KeyValuePair<string,string>> FindTopicsByInteractor(IInteractor interactor)
        {
            foreach (var subscriptionToFeed in _cache)
                foreach (var subscriptionToTopic in subscriptionToFeed.Value.Where(x => x.Value.Contains(interactor)))
                    yield return KeyValuePair.Create(subscriptionToFeed.Key, subscriptionToTopic.Key);
        }

        // TODO: Rename
        public bool RemoveTopic(string feed, string topic)
        {
            // Find the subscribers to topics for this feed.
            IDictionary<string, CountedSet<IInteractor>> subscribersForFeed;
            if (!_cache.TryGetValue(feed, out subscribersForFeed))
                return false;

            return subscribersForFeed.Remove(topic);
        }

        // TODO: Rename
        public CountedSet<IInteractor> GetSubscribersToTopic(string feed, string topic)
        {
            // Find the subscribers to topics for this feed.
            IDictionary<string, CountedSet<IInteractor>> subscribersForFeed;
            if (!_cache.TryGetValue(feed, out subscribersForFeed))
                return null;

            // Are there subscribers for this topic?
            CountedSet<IInteractor> subscribersForTopic;
            if (!subscribersForFeed.TryGetValue(topic, out subscribersForTopic))
                return null;

            return subscribersForTopic;
        }

        // TODO: Rename, remove regex.
        public IEnumerable<KeyValuePair<string, CountedSet<IInteractor>>> GetSubscribersToFeed(string feed)
        {
            // Find the subscribers to topics for this feed.
            IDictionary<string, CountedSet<IInteractor>> subscribersForFeed;
            if (!_cache.TryGetValue(feed, out subscribersForFeed))
                yield break;
            else
                foreach (var subscription in subscribersForFeed)
                    yield return subscription;
        }
    }
}
