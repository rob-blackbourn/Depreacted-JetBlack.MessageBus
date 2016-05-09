using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class SubscriptionRepository
    {
        // Topic->Interactor->SubscriptionCount.
        private readonly IDictionary<string, CountedSet<Interactor>> _cache = new Dictionary<string, CountedSet<Interactor>>();

        public void AddSubscription(Interactor subscriber, string topic)
        {
            // Find the list of interactors that have subscribed to this topic.
            CountedSet<Interactor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                _cache.Add(topic, new CountedSet<Interactor>(new[] { subscriber }));
            else
                subscribersForTopic.Add(subscriber);
        }

        public void RemoveSubscription(Interactor subscriber, string topic)
        {
            // Can we find this topic in the cache?
            CountedSet<Interactor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                return;

            // Has this subscriber registered an interest in the topic?
            if (!subscribersForTopic.Contains(subscriber))
                return;

            subscribersForTopic.Remove(subscriber);

            // If there are no subscribers left on this topic, remove it from the cache.
            if (subscribersForTopic.Count == 0)
                _cache.Remove(topic);
        }

        public IEnumerable<KeyValuePair<string, CountedSet<Interactor>>> FindTopicsByInteractor(Interactor interactor)
        {
            return _cache.Where(x => x.Value.Contains(interactor));
        }

        public bool RemoveTopic(string topic)
        {
            return _cache.Remove(topic);
        }

        public CountedSet<Interactor> GetSubscribersToTopic(string topic)
        {
            // Are there subscribers for this topic?
            CountedSet<Interactor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                return null;
            return subscribersForTopic;
        }

        public IEnumerable<KeyValuePair<string, CountedSet<Interactor>>> GetSubscribersMatchingTopic(Regex topicRegex)
        {
            return _cache.Where(x => topicRegex.IsMatch(x.Key));
        }
    }
}
