using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class SubscriptionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly NotificationMarshaller _notificationMarshaller;
        private readonly PublisherMarshaller _publisherMarshaller;

        public SubscriptionManager(NotificationMarshaller notificationMarshaller, PublisherMarshaller publisherMarshaller)
        {
            _notificationMarshaller = notificationMarshaller;
            _publisherMarshaller = publisherMarshaller;
        }

        private readonly SubscriptionRepository _cache = new SubscriptionRepository();

        public void RequestSubscription(Interactor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", subscriber, subscriptionRequest);

            if (subscriptionRequest.IsAdd)
                _cache.AddSubscription(subscriber, subscriptionRequest.Topic);
            else
                _cache.RemoveSubscription(subscriber, subscriptionRequest.Topic);

            _notificationMarshaller.ForwardSubscription(subscriber, subscriptionRequest);
        }

        public void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);

            OnClosedInteractor(interactor);
        }

        public void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing subscriptions for {0}", interactor);

            // Remove the subscriptions
            var topicsSubscribedTo = new List<string>();
            var topicsWithoutSubscribers = new List<string>();
            foreach (var subscription in _cache.FindTopicsByInteractor(interactor))
            {
                topicsSubscribedTo.Add(subscription.Key);

                subscription.Value.Remove(interactor);
                if (subscription.Value.Count == 0)
                    topicsWithoutSubscribers.Add(subscription.Key);
            }

            foreach (var topic in topicsWithoutSubscribers)
                _cache.RemoveTopic(topic);

            // Inform those interested that this interactor is no longer subscribed to these topics.
            foreach (var subscriptionRequest in topicsSubscribedTo.Select(topic => new SubscriptionRequest(topic, false)))
                _notificationMarshaller.ForwardSubscription(interactor, subscriptionRequest);
        }

        public void SendUnicastData(Interactor publisher, UnicastData unicastData)
        {
            // Are there subscribers for this topic?
            var subscribersForTopic = _cache.GetSubscribersToTopic(unicastData.Topic);
            if (subscribersForTopic == null)
                return;

            // Can we find this client in the subscribers to this topic?
            var subscriber = subscribersForTopic.FirstOrDefault(x => x.Id == unicastData.ClientId);
            if (subscriber == null)
                return;

            _publisherMarshaller.SendUnicastData(publisher, subscriber, unicastData);
        }

        public void SendMulticastData(Interactor publisher, MulticastData multicastData)
        {
            // Are there subscribers for this topic?
            var subscribersForTopic = _cache.GetSubscribersToTopic(multicastData.Topic);
            if (subscribersForTopic == null)
                return;

            _publisherMarshaller.SendMulticastData(publisher, subscribersForTopic, multicastData);
        }

        public void OnNewNotificationRequest(Interactor requester, Regex topicRegex)
        {
            // Find the subscribers whoes subscriptions match the pattern.
            foreach (var matchingSubscriptions in _cache.GetSubscribersMatchingTopic(topicRegex))
            {
                // Tell the requestor about subscribers that are interested in this topic.
                foreach (var subscriber in matchingSubscriptions.Value)
                    requester.SendMessage(new ForwardedSubscriptionRequest(subscriber.Id, matchingSubscriptions.Key, true));
            }
        }

        public void OnStaleTopics(IEnumerable<string> staleTopics)
        {
            foreach (var staleTopic in staleTopics)
                OnStaleTopic(staleTopic);
        }

        private void OnStaleTopic(string staleTopic)
        {
            var subscribersForTopic = _cache.GetSubscribersToTopic(staleTopic);
            if (subscribersForTopic == null)
                return;

            // Inform subscribers by sending an image with no data.
            var staleMessage = new MulticastData(staleTopic, true, null);
            foreach (var subscriber in subscribersForTopic)
                subscriber.SendMessage(staleMessage);
        }
    }
}
