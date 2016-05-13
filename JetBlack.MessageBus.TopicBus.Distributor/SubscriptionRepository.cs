using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class SubscriptionRepository
    {
        // Topic->Interactor->SubscriptionCount.
        private readonly IDictionary<string, CountedSet<IInteractor>> _cache = new Dictionary<string, CountedSet<IInteractor>>();

        public void AddSubscription(IInteractor subscriber, string topic)
        {
            // Find the list of interactors that have subscribed to this topic.
            CountedSet<IInteractor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                _cache.Add(topic, new CountedSet<IInteractor>(new[] { subscriber }));
            else
                subscribersForTopic.Add(subscriber);
        }

        public void RemoveSubscription(IInteractor subscriber, string topic, bool removeAll)
        {
            // Can we find this topic in the cache?
            CountedSet<IInteractor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                return;

            // Has this subscriber registered an interest in the topic?
            if (!subscribersForTopic.Contains(subscriber))
                return;

            if (removeAll)
                subscribersForTopic.RemoveAll(subscriber);
            else
                subscribersForTopic.Remove(subscriber);

            // If there are no subscribers left on this topic, remove it from the cache.
            if (subscribersForTopic.Count == 0)
                _cache.Remove(topic);
        }

        public IEnumerable<string> FindTopicsByInteractor(IInteractor interactor)
        {
            return _cache.Where(x => x.Value.Contains(interactor)).Select(x => x.Key);
        }

        public bool RemoveTopic(string topic)
        {
            return _cache.Remove(topic);
        }

        public CountedSet<IInteractor> GetSubscribersToTopic(string topic)
        {
            // Are there subscribers for this topic?
            CountedSet<IInteractor> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                return null;
            return subscribersForTopic;
        }

        public IEnumerable<KeyValuePair<string, CountedSet<IInteractor>>> GetSubscribersMatchingTopic(Regex topicRegex)
        {
            return _cache.Where(x => topicRegex.IsMatch(x.Key));
        }
    }
}
