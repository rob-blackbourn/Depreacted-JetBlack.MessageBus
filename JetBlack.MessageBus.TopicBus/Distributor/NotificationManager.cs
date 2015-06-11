﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.Common.Collections;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class NotificationManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, ISet<Interactor>> _topicPatternToNotifiables = new Dictionary<string, ISet<Interactor>>();
        private readonly Dictionary<string, Regex> _topicPatternToRegex = new Dictionary<string, Regex>();

        private readonly ISubject<ForwardedSubscriptionRequest> _forwardedSubscriptionRequests = new Subject<ForwardedSubscriptionRequest>();
        private readonly ISubject<SourceMessage<NotificationRequest>> _notificationRequests = new Subject<SourceMessage<NotificationRequest>>();
        private readonly ISubject<SourceMessage<Regex>> _newNotificationRequests = new Subject<SourceMessage<Regex>>();

        private readonly IDisposable _disposable;

        public NotificationManager(InteractorManager interactorManager)
        {
            _notificationRequests = new Subject<SourceMessage<NotificationRequest>>();

            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _notificationRequests.ObserveOn(scheduler).Subscribe(x => OnNotificationRequest(x.Source, x.Content)),
                    _forwardedSubscriptionRequests.ObserveOn(scheduler).Subscribe(OnForwardedSubscriptionRequest),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public IObservable<SourceMessage<Regex>> NewNotificationRequests
        {
            get { return _newNotificationRequests; }
        }

        public IObserver<ForwardedSubscriptionRequest> ForwardedSubscriptionRequests
        {
            get { return _forwardedSubscriptionRequests; }
        }

        public void ForwardSubscription(Interactor subscriber, SubscriptionRequest subscriptionRequest)
        {
            _forwardedSubscriptionRequests.OnNext(new ForwardedSubscriptionRequest(subscriber.Id, subscriptionRequest.Topic, subscriptionRequest.IsAdd));
        }

        public void RequestNotification(Interactor notifiable, NotificationRequest notificationRequest)
        {
            _notificationRequests.OnNext(SourceMessage.Create(notifiable, notificationRequest));
        }

        private void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing notification requests from {0}", interactor);

            // Remove the interactor where it appears in the notifiables, remembering any topics which are left without any interactors.
            var topicsWithoutInteractors = new HashSet<string>();
            foreach (var topicPatternToNotifiable in _topicPatternToNotifiables.Where(x => x.Value.Contains(interactor)))
            {
                topicPatternToNotifiable.Value.Remove(interactor);
                if (topicPatternToNotifiable.Value.Count == 0)
                    topicsWithoutInteractors.Add(topicPatternToNotifiable.Key);
            }

            // Remove any topics left without interactors.
            foreach (var topic in topicsWithoutInteractors)
            {
                _topicPatternToNotifiables.Remove(topic);
                _topicPatternToRegex.Remove(topic);
            }
        }

        private void OnNotificationRequest(Interactor notifiable, NotificationRequest notificationRequest)
        {
            Log.DebugFormat("Handling notification request for {0} on {1}", notifiable, notificationRequest);

            if (notificationRequest.IsAdd)
                AddNotificationRequest(notifiable, notificationRequest.TopicPattern);
            else
                RemoveNotificationRequest(notifiable, notificationRequest.TopicPattern);
        }

        private void AddNotificationRequest(Interactor notifiable, string topicPattern)
        {
            // Find or create the set of notifiables for this topic, and cache the regex for the topic pattern.
            ISet<Interactor> notifiables;
            Regex topicRegex;
            if (!_topicPatternToNotifiables.TryGetValue(topicPattern, out notifiables))
            {
                _topicPatternToNotifiables.Add(topicPattern, notifiables = new HashSet<Interactor>());
                _topicPatternToRegex.Add(topicPattern, topicRegex = new Regex(topicPattern));
            }
            else if (!notifiables.Contains(notifiable))
                topicRegex = _topicPatternToRegex[topicPattern];
            else
                return;

            // Add to the notifiables for this topic pattern and inform the subscription manager of the new notification request.
            notifiables.Add(notifiable);
            _newNotificationRequests.OnNext(SourceMessage.Create(notifiable, topicRegex));
        }

        private void RemoveNotificationRequest(Interactor notifiable, string topicPattern)
        {
            // Does this topic pattern have any notifiable interactors?
            ISet<Interactor> notifiables;
            if (!_topicPatternToNotifiables.TryGetValue(topicPattern, out notifiables))
                return;
            
            // Is this interactor in the set of notifiables for this topic pattern?
            if (!notifiables.Contains(notifiable))
                return;

            // Remove the interactor from the set of notifiables.
            notifiables.Remove(notifiable);

            // Are there any interactors left listening to this topic pattern?
            if (notifiables.Count != 0)
                return;

            // Remove the empty pattern from the caches.
            _topicPatternToNotifiables.Remove(topicPattern);
            _topicPatternToRegex.Remove(topicPattern);
        }

        private void OnForwardedSubscriptionRequest(ForwardedSubscriptionRequest forwardedSubscriptionRequest)
        {
            // Find all the interactors that wish to be notified of subscriptions to this topic.
            var notifiables = _topicPatternToRegex
                .Where(x => x.Value.IsMatch(forwardedSubscriptionRequest.Topic))
                .SelectMany(x => _topicPatternToNotifiables[x.Key])
                .ToSet();

            Log.DebugFormat("Notifying interactors[{0}] of subscription {1}", string.Join(",", notifiables), forwardedSubscriptionRequest);

            // Inform each notifiable interactor of the subscription request.
            foreach (var notifiable in notifiables)
                notifiable.SendMessage(forwardedSubscriptionRequest);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
