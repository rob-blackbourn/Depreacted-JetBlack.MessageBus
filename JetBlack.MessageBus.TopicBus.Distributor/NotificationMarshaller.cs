using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using log4net;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class NotificationMarshaller : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly NotificationManager _notificationManager;
        private readonly ISubject<ForwardedSubscriptionRequest> _forwardedSubscriptionRequests = new Subject<ForwardedSubscriptionRequest>();
        private readonly ISubject<SourceMessage<NotificationRequest>> _notificationRequests = new Subject<SourceMessage<NotificationRequest>>();

        private readonly IDisposable _disposable;

        public NotificationMarshaller(InteractorManager interactorManager)
        {
            _notificationRequests = new Subject<SourceMessage<NotificationRequest>>();
            _notificationManager = new NotificationManager();

            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _notificationRequests.ObserveOn(scheduler).Subscribe(x => _notificationManager.RequestNotification(x.Source, x.Content)),
                    _forwardedSubscriptionRequests.ObserveOn(scheduler).Subscribe(_notificationManager.ForwardSubscription),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(_notificationManager.OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => _notificationManager.OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public IObservable<SourceMessage<Regex>> NewNotificationRequests
        {
            get { return _notificationManager.NewNotificationRequests; }
        }

        public IObserver<ForwardedSubscriptionRequest> ForwardedSubscriptionRequests
        {
            get { return _forwardedSubscriptionRequests; }
        }

        public void ForwardSubscription(IInteractor subscriber, SubscriptionRequest subscriptionRequest)
        {
            _forwardedSubscriptionRequests.OnNext(new ForwardedSubscriptionRequest(subscriber.Id, subscriptionRequest.Topic, subscriptionRequest.IsAdd));
        }

        public void RequestNotification(IInteractor notifiable, NotificationRequest notificationRequest)
        {
            _notificationRequests.OnNext(SourceMessage.Create(notifiable, notificationRequest));
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
