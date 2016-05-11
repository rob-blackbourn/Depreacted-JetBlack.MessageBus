using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using log4net;
using JetBlack.MessageBus.TopicBus.Messages;

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

        private readonly SubscriptionRepository _repository = new SubscriptionRepository();

        public void RequestSubscription(Interactor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", subscriber, subscriptionRequest);

            if (subscriptionRequest.IsAdd)
                _repository.AddSubscription(subscriber, subscriptionRequest.Topic);
            else
                _repository.RemoveSubscription(subscriber, subscriptionRequest.Topic, false);

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
            var topics = _repository.FindTopicsByInteractor(interactor).ToList();
            foreach (var topic in topics)
                _repository.RemoveSubscription(interactor, topic, true);

            // Inform those interested that this interactor is no longer subscribed to these topics.
            foreach (var subscriptionRequest in topics.Select(topic => new SubscriptionRequest(topic, false)))
                _notificationMarshaller.ForwardSubscription(interactor, subscriptionRequest);
        }

        public void SendUnicastData(Interactor publisher, UnicastData unicastData)
        {
            // Are there subscribers for this topic?
            var subscribersForTopic = _repository.GetSubscribersToTopic(unicastData.Topic);
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
            var subscribersForTopic = _repository.GetSubscribersToTopic(multicastData.Topic);
            if (subscribersForTopic == null)
                return;

            _publisherMarshaller.SendMulticastData(publisher, subscribersForTopic, multicastData);
        }

        public void OnNewNotificationRequest(Interactor requester, Regex topicRegex)
        {
            // Find the subscribers whoes subscriptions match the pattern.
            foreach (var matchingSubscriptions in _repository.GetSubscribersMatchingTopic(topicRegex))
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
            var subscribersForTopic = _repository.GetSubscribersToTopic(staleTopic);
            if (subscribersForTopic == null)
                return;

            // Inform subscribers by sending an image with no data.
            var staleMessage = new MulticastData(staleTopic, true, null);
            foreach (var subscriber in subscribersForTopic)
                subscriber.SendMessage(staleMessage);
        }
    }
}
