using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class SubscriptionManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // Topic->Interactor->SubscriptionCount.
        private readonly IDictionary<string, IDictionary<Interactor, int>> _cache = new Dictionary<string, IDictionary<Interactor, int>>();

        private readonly NotificationManager _notificationManager;
        private readonly PublisherManager _publisherManager;
        private readonly ISubject<SourceMessage<SubscriptionRequest>> _subscriptionRequests = new Subject<SourceMessage<SubscriptionRequest>>();
        private readonly ISubject<SourceMessage<MulticastData>> _publishedMulticastDataMessages = new Subject<SourceMessage<MulticastData>>();
        private readonly ISubject<SourceMessage<UnicastData>> _publishedUnicastDataMessages = new Subject<SourceMessage<UnicastData>>();
        private readonly IDisposable _disposable;

        public SubscriptionManager(InteractorManager interactorManager, NotificationManager notificationManager, PublisherManager publisherManager)
        {
            _notificationManager = notificationManager;
            _publisherManager = publisherManager;

            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _subscriptionRequests.ObserveOn(scheduler).Subscribe(x => OnSubscriptionRequest(x.Source, x.Content)),
                    _notificationManager.NewNotificationRequests.ObserveOn(scheduler).Subscribe(x => OnNewNotificationRequest(x.Source, x.Content)),
                    _publishedMulticastDataMessages.ObserveOn(scheduler).Subscribe(x => OnMulticastDataPublished(x.Source, x.Content)),
                    _publishedUnicastDataMessages.ObserveOn(scheduler).Subscribe(x => OnUnicastDataPublished(x.Source, x.Content)),
                    _publisherManager.StalePublishers.ObserveOn(scheduler).Subscribe(x => OnStaleTopics(x.Content)),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public void SendMulticastData(Interactor sender, MulticastData multicastData)
        {
            Log.DebugFormat("SendMulticastData(sender={0}, multicastData={1})", sender, multicastData);

            _publishedMulticastDataMessages.OnNext(SourceMessage.Create(sender, multicastData));
        }

        public void SendUnicastData(Interactor sender, UnicastData unicastData)
        {
            Log.DebugFormat("SendUnicastData(sender={0}, unicastData={1})", sender, unicastData);

            _publishedUnicastDataMessages.OnNext(SourceMessage.Create(sender, unicastData));
        }

        public void RequestSubscription(Interactor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("RequestSubscription(sender={0}, request={1})", subscriber, subscriptionRequest);

            _subscriptionRequests.OnNext(SourceMessage.Create(subscriber, subscriptionRequest));
        }

        private void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);

            OnClosedInteractor(interactor);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing subscriptions for {0}", interactor);

            // Remove the subscriptions
            var topicsSubscribedTo = new List<string>();
            var topicsWithoutSubscribers = new List<string>();
            foreach (var subscription in _cache.Where(x => x.Value.ContainsKey(interactor)))
            {
                topicsSubscribedTo.Add(subscription.Key);

                subscription.Value.Remove(interactor);
                if (subscription.Value.Count == 0)
                    topicsWithoutSubscribers.Add(subscription.Key);
            }

            foreach (var topic in topicsWithoutSubscribers)
                _cache.Remove(topic);

            // Inform those interested that this interactor is no longer subscribed to these topics.
            foreach (var subscriptionRequest in topicsSubscribedTo.Select(topic => new SubscriptionRequest(topic, false)))
                _notificationManager.ForwardSubscription(interactor, subscriptionRequest);
        }

        private void OnUnicastDataPublished(Interactor publisher, UnicastData unicastData)
        {
            // Are there subscribers for this topic?
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(unicastData.Topic, out subscribers))
                return;

            // Can we find this client in the subscribers to this topic?
            var subscriber = subscribers.FirstOrDefault(x => x.Key.Id == unicastData.ClientId).Key;
            if (subscriber == null)
                return;

            _publisherManager.Send(publisher, subscriber, unicastData);
        }

        private void OnMulticastDataPublished(Interactor publisher, MulticastData multicastData)
        {
            // Are there subscribers for this topic?
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(multicastData.Topic, out subscribers))
                return;

            _publisherManager.Send(publisher, subscribers.Keys, multicastData);
        }

        private void OnSubscriptionRequest(Interactor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", subscriber, subscriptionRequest);

            if (subscriptionRequest.IsAdd)
                AddSubscription(subscriber, subscriptionRequest.Topic);
            else
                RemoveSubscription(subscriber, subscriptionRequest.Topic);
        }

        private void AddSubscription(Interactor subscriber, string topic)
        {
            // Find the list of interactors that have subscribed to this topic.
            IDictionary<Interactor, int> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                _cache.Add(topic, new Dictionary<Interactor, int> {{subscriber, 1}});
            else if (!subscribersForTopic.ContainsKey(subscriber))
                subscribersForTopic.Add(subscriber, 1);
            else
                ++subscribersForTopic[subscriber];
        }

        private void RemoveSubscription(Interactor subscriber, string topic)
        {
            // Can we find this topic in the cache?
            IDictionary<Interactor, int> subscribersForTopic;
            if (!_cache.TryGetValue(topic, out subscribersForTopic))
                return;

            // Has this subscriber registered an interest in the topic?
            if (!subscribersForTopic.ContainsKey(subscriber))
                return;

            // Decrement the subscription count, and if there are none left, remove it.
            if (--subscribersForTopic[subscriber] == 0)
                subscribersForTopic.Remove(subscriber);

            // If there are no subscribers left on this topic, remove it from the cache.
            if (subscribersForTopic.Count == 0)
                _cache.Remove(topic);
        }

        private void OnNewNotificationRequest(Interactor requester, Regex topicRegex)
        {
            // Find the subscribers whoes subscriptions match the pattern.
            foreach (var matchingSubscriptions in _cache.Where(x => topicRegex.IsMatch(x.Key)))
                ForwardSubscriptionRequest(requester, matchingSubscriptions.Key, matchingSubscriptions.Value.Keys);
        }

        private static void ForwardSubscriptionRequest(Interactor requester, string topic, IEnumerable<Interactor> subscribers)
        {
            // Tell the requestor about subscribers that are interested in this topic.
            foreach (var subscriber in subscribers)
                requester.SendMessage(new ForwardedSubscriptionRequest(subscriber.Id, topic, true));
        }

        private void OnStaleTopics(IEnumerable<string> staleTopics)
        {
            foreach (var staleTopic in staleTopics)
                OnStaleTopic(staleTopic);
        }

        private void OnStaleTopic(string staleTopic)
        {
            IDictionary<Interactor, int> subscribersForTopic;
            if (!_cache.TryGetValue(staleTopic, out subscribersForTopic))
                return;

            // Inform subscribers by sending an image with no data.
            var staleMessage = new MulticastData(staleTopic, true, null);
            foreach (var subscriber in subscribersForTopic.Keys)
                subscriber.SendMessage(staleMessage);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
