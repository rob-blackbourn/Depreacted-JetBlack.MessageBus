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
    internal class NotificationManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, Notification> _cache = new Dictionary<string, Notification>();
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
                    _notificationRequests.ObserveOn(scheduler).Subscribe(OnNotificationRequest),
                    _forwardedSubscriptionRequests.ObserveOn(scheduler).Subscribe(OnForwardedSubscriptionRequest),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(OnFaultedInteractor)
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

        public void NotifySubscriptionRequest(int id, string topic, bool isAdd)
        {
            _forwardedSubscriptionRequests.OnNext(new ForwardedSubscriptionRequest(id, topic, isAdd));
        }

        public void RequestNotification(Interactor sender, NotificationRequest notificationRequest)
        {
            _notificationRequests.OnNext(SourceMessage.Create(sender, notificationRequest));
        }

        private void OnFaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);
            OnClosedInteractor(sourceMessage.Source);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing notification requests from {0}", interactor);

            var emptyNotifications = new List<string>();
            foreach (var item in _cache.Where(x => x.Value.Notifiables.Contains(interactor)))
            {
                item.Value.Notifiables.Remove(interactor);
                if (item.Value.Notifiables.Count == 0)
                    emptyNotifications.Add(item.Key);
            }

            foreach (var topic in emptyNotifications)
                _cache.Remove(topic);
        }

        private void OnNotificationRequest(SourceMessage<NotificationRequest> sourceMessage)
        {
            Log.DebugFormat("Handling notification request for {0} on {1}", sourceMessage.Source, sourceMessage.Content);

            if (sourceMessage.Content.IsAdd)
                AddNotificationRequest(sourceMessage.Source, sourceMessage.Content);
            else
                RemoveNotificationRequest(sourceMessage.Source, sourceMessage.Content);
        }

        private void AddNotificationRequest(Interactor source, NotificationRequest notificationRequest)
        {
            Notification notification;
            if (!_cache.TryGetValue(notificationRequest.TopicPattern, out notification))
                _cache.Add(notificationRequest.TopicPattern, notification = new Notification(new Regex(notificationRequest.TopicPattern)));

            if (!notification.Notifiables.Contains(source))
            {
                notification.Notifiables.Add(source);
                _newNotificationRequests.OnNext(SourceMessage.Create(source, notification.TopicPattern));
            }
        }

        public void RemoveNotificationRequest(Interactor source, NotificationRequest notificationRequest)
        {
            Notification notification;
            if (_cache.TryGetValue(notificationRequest.TopicPattern, out notification) && notification.Notifiables.Contains(source))
            {
                notification.Notifiables.Remove(source);
                if (notification.Notifiables.Count == 0)
                    _cache.Remove(notificationRequest.TopicPattern);
            }
        }

        private void OnForwardedSubscriptionRequest(ForwardedSubscriptionRequest message)
        {
            var notifiables = _cache.Values
                .Where(x => x.TopicPattern.Matches(message.Topic).Count != 0)
                .SelectMany(x => x.Notifiables)
                .ToList();

            Log.DebugFormat("Notifying interactors[{0}] of subscription {1}", string.Join(",", notifiables), message);

            foreach (var notifiable in notifiables)
                notifiable.SendMessage(message);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }

        private class Notification
        {
            internal readonly ISet<Interactor> Notifiables = new HashSet<Interactor>();
            internal readonly Regex TopicPattern;

            internal Notification(Regex topicPattern)
            {
                TopicPattern = topicPattern;
            }
        }
    }
}
