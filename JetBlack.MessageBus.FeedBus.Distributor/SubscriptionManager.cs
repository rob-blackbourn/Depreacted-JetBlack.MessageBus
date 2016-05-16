using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using log4net;
using JetBlack.MessageBus.FeedBus.Messages;
using JetBlack.MessageBus.FeedBus.Distributor.Config;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class SubscriptionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SubscriptionRepository _repository = new SubscriptionRepository();
        private readonly NotificationMarshaller _notificationMarshaller;
        private readonly PublisherMarshaller _publisherMarshaller;

        public SubscriptionManager(NotificationMarshaller notificationMarshaller, PublisherMarshaller publisherMarshaller)
        {
            _notificationMarshaller = notificationMarshaller;
            _publisherMarshaller = publisherMarshaller;
        }

        public void RequestSubscription(IInteractor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", subscriber, subscriptionRequest);

            if (!subscriber.HasRole(subscriptionRequest.Feed, ClientRole.Subscribe))
            {
                Log.WarnFormat("Subscription denied for client \"{0}\" on feed \"{1}\"", subscriber, subscriptionRequest.Feed);
                return;
            }

            if (subscriptionRequest.IsAdd)
                _repository.AddSubscription(subscriber, subscriptionRequest.Feed, subscriptionRequest.Topic);
            else
                _repository.RemoveSubscription(subscriber, subscriptionRequest.Feed, subscriptionRequest.Topic, false);

            _notificationMarshaller.ForwardSubscription(subscriber, subscriptionRequest);
        }

        public void OnFaultedInteractor(IInteractor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);

            OnClosedInteractor(interactor);
        }

        public void OnClosedInteractor(IInteractor interactor)
        {
            Log.DebugFormat("Removing subscriptions for {0}", interactor);

            // Remove the subscriptions
            var feedsAndTopics = _repository.FindTopicsByInteractor(interactor).ToList();
            foreach (var feedAndTopic in feedsAndTopics)
                _repository.RemoveSubscription(interactor, feedAndTopic.Key, feedAndTopic.Value, true);

            // Inform those interested that this interactor is no longer subscribed to these topics.
            foreach (var subscriptionRequest in feedsAndTopics.Select(feedAndTopic => new SubscriptionRequest(feedAndTopic.Key, feedAndTopic.Value, false)))
                _notificationMarshaller.ForwardSubscription(interactor, subscriptionRequest);
        }

        public void SendUnicastData(IInteractor publisher, UnicastData unicastData)
        {
            // Are there subscribers for this topic?
            var subscribersForTopic = _repository.GetSubscribersToTopic(unicastData.Feed, unicastData.Topic);
            if (subscribersForTopic == null)
                return;

            // Can we find this client in the subscribers to this topic?
            var subscriber = subscribersForTopic.FirstOrDefault(x => x.Id == unicastData.ClientId);
            if (subscriber == null)
                return;

            _publisherMarshaller.SendUnicastData(publisher, subscriber, unicastData);
        }

        public void SendMulticastData(IInteractor publisher, MulticastData multicastData)
        {
            // Are there subscribers for this topic?
            var subscribersForTopic = _repository.GetSubscribersToTopic(multicastData.Feed, multicastData.Topic);
            if (subscribersForTopic == null)
                return;

            _publisherMarshaller.SendMulticastData(publisher, subscribersForTopic, multicastData);
        }

        public void OnNewNotificationRequest(IInteractor requester, string feed)
        {
            // Find the subscribers whoes subscriptions match the pattern.
            foreach (var matchingSubscriptions in _repository.GetSubscribersToFeed(feed))
            {
                // Tell the requestor about subscribers that are interested in this topic.
                foreach (var subscriber in matchingSubscriptions.Value)
                    requester.SendMessage(new ForwardedSubscriptionRequest(subscriber.Id, subscriber.IPAddress, subscriber.Name, feed, matchingSubscriptions.Key, true));
            }
        }

        // TODO: This should only need feed.
        public void OnStaleTopics(IEnumerable<FeedAndTopic> staleFeedsAndTopics)
        {
            foreach (var staleFeedAndTopic in staleFeedsAndTopics)
                OnStaleTopic(staleFeedAndTopic.Feed, staleFeedAndTopic.Topic);
        }

        // TODO: This should only need feed.
        private void OnStaleTopic(string staleFeed, string staleTopic)
        {
            var subscribersForTopic = _repository.GetSubscribersToTopic(staleFeed, staleTopic);
            if (subscribersForTopic == null)
                return;

            // Inform subscribers by sending an image with no data.
            var staleMessage = new MulticastData(staleFeed, staleTopic, true, null);
            foreach (var subscriber in subscribersForTopic)
                subscriber.SendMessage(staleMessage);
        }
    }
}
