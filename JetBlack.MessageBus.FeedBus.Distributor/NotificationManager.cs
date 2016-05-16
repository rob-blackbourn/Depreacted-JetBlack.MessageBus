using System;
using System.Reactive.Subjects;
using log4net;
using JetBlack.MessageBus.FeedBus.Messages;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class NotificationManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly NotificationRepository _repository = new NotificationRepository();
        private readonly ISubject<SourceMessage<string>> _newNotificationRequests = new Subject<SourceMessage<string>>();

        public IObservable<SourceMessage<string>> NewNotificationRequests
        {
            get { return _newNotificationRequests; }
        }

        public void OnFaultedInteractor(IInteractor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }

        public void OnClosedInteractor(IInteractor interactor)
        {
            Log.DebugFormat("Removing notification requests from {0}", interactor);
            _repository.RemoveInteractor(interactor);
        }

        public void RequestNotification(IInteractor notifiable, NotificationRequest notificationRequest)
        {
            Log.DebugFormat("Handling notification request for {0} on {1}", notifiable, notificationRequest);

            if (notificationRequest.IsAdd)
                _repository.AddRequest(notifiable, notificationRequest.Feed, _newNotificationRequests);
            else
                _repository.RemoveRequest(notifiable, notificationRequest.Feed);
        }

        public void ForwardSubscription(ForwardedSubscriptionRequest forwardedSubscriptionRequest)
        {
            // Find all the interactors that wish to be notified of subscriptions to this topic.
            var notifiables = _repository.FindNotifiables(forwardedSubscriptionRequest.Topic);

            Log.DebugFormat("Notifying interactors[{0}] of subscription {1}", string.Join(",", notifiables), forwardedSubscriptionRequest);

            // Inform each notifiable interactor of the subscription request.
            foreach (var notifiable in notifiables)
                notifiable.SendMessage(forwardedSubscriptionRequest);
        }
    }
}
