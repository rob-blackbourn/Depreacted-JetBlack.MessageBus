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
                    _subscriptionRequests.ObserveOn(scheduler).Subscribe(OnSubscriptionRequest),
                    _notificationManager.NewNotificationRequests.ObserveOn(scheduler).Subscribe(OnNewNotificationRequest),
                    _publishedMulticastDataMessages.ObserveOn(scheduler).Subscribe(OnMulticastDataPublished),
                    _publishedUnicastDataMessages.ObserveOn(scheduler).Subscribe(OnUnicastDataPublished),
                    _publisherManager.StalePublishers.ObserveOn(scheduler).Subscribe(OnStalePublisher),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(OnFaultedInteractor)
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

        public void AddSubscription(Interactor sender, SubscriptionRequest request)
        {
            Log.DebugFormat("AddSubscription(sender={0}, request={1})", sender, request);

            _subscriptionRequests.OnNext(SourceMessage.Create(sender, request));
        }

        private void OnFaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);

            OnClosedInteractor(sourceMessage.Source);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing subscriptions for {0}", interactor);

            // Remove the subscriptions
            var subscriptions = new List<string>();
            var emptySubscriptions = new List<string>();
            foreach (var item in _cache.Where(x => x.Value.ContainsKey(interactor)))
            {
                item.Value.Remove(interactor);
                if (item.Value.Count == 0)
                    emptySubscriptions.Add(item.Key);
                subscriptions.Add(item.Key);
            }

            foreach (var topic in subscriptions)
                _notificationManager.NotifySubscriptionRequest(interactor.Id, topic, false);

            foreach (var topic in emptySubscriptions)
                _cache.Remove(topic);
        }

        private void OnUnicastDataPublished(SourceMessage<UnicastData> sourceMessage)
        {
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(sourceMessage.Content.Topic, out subscribers))
                return;

            var subscriber = subscribers.FirstOrDefault(x => x.Key.Id == sourceMessage.Content.ClientId).Key;
            if (subscriber != null)
                _publisherManager.Send(sourceMessage.Source, subscriber, sourceMessage.Content);
        }

        private void OnMulticastDataPublished(SourceMessage<MulticastData> sourceMessage)
        {
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(sourceMessage.Content.Topic, out subscribers))
                return;

            foreach (var subscriber in subscribers.Keys)
                _publisherManager.Send(sourceMessage.Source, subscriber, sourceMessage.Content);
        }

        private void OnSubscriptionRequest(SourceMessage<SubscriptionRequest> sourceMessage)
        {
            Log.DebugFormat("Received subscription from {0} on \"{1}\"", sourceMessage.Source, sourceMessage.Content);

            if (sourceMessage.Content.IsAdd)
                AddSubscription(sourceMessage.Source, sourceMessage.Content.Topic);
            else
                RemoveSubscription(sourceMessage.Source, sourceMessage.Content.Topic);
        }

        private void AddSubscription(Interactor subscriber, string topic)
        {
            // Find the list of interactors that have subscribed to this topic.
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(topic, out subscribers))
                _cache.Add(topic, (subscribers = new Dictionary<Interactor, int>()));

            if (subscribers.ContainsKey(subscriber))
                ++subscribers[subscriber];
            else
                subscribers.Add(subscriber, 1);
        }

        private void RemoveSubscription(Interactor subscriber, string topic)
        {
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(topic, out subscribers))
                return;

            if (subscribers.ContainsKey(subscriber))
            {
                if (--subscribers[subscriber] <= 0)
                    subscribers.Remove(subscriber);
            }

            if (subscribers.Count == 0)
                _cache.Remove(topic);
        }

        private void OnNewNotificationRequest(SourceMessage<Regex> sourceMessage)
        {
            foreach (var item in _cache.Where(x => sourceMessage.Content.Match(x.Key).Success))
            {
                Log.DebugFormat("Notification pattern {0} matched [{1}] subscribers", sourceMessage.Content, string.Join(",", item.Value));

                foreach (var subscriber in item.Value.Keys)
                    sourceMessage.Source.SendMessage(new ForwardedSubscriptionRequest(subscriber.Id, item.Key, true));
            }
        }

        private void OnStalePublisher(SourceMessage<IEnumerable<string>> forwardedMessage)
        {
            foreach (var staleTopic in forwardedMessage.Content)
                OnStalePublisher(staleTopic);
        }

        private void OnStalePublisher(string staleTopic)
        {
            IDictionary<Interactor, int> subscribers;
            if (!_cache.TryGetValue(staleTopic, out subscribers))
                return;

            var staleMessage = new MulticastData(staleTopic, true, null);
            foreach (var subscriber in subscribers.Keys)
                subscriber.SendMessage(staleMessage);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
