using System;
using System.Reactive.Subjects;
using System.Text.RegularExpressions;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class NotificationManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly NotificationRepository _repository = new NotificationRepository();
        private readonly ISubject<SourceMessage<Regex>> _newNotificationRequests = new Subject<SourceMessage<Regex>>();

        public IObservable<SourceMessage<Regex>> NewNotificationRequests
        {
            get { return _newNotificationRequests; }
        }

        public void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }

        public void OnClosedInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing notification requests from {0}", interactor);
            _repository.RemoveInteractor(interactor);
        }

        public void RequestNotification(Interactor notifiable, NotificationRequest notificationRequest)
        {
            Log.DebugFormat("Handling notification request for {0} on {1}", notifiable, notificationRequest);

            if (notificationRequest.IsAdd)
                _repository.AddRequest(notifiable, notificationRequest.TopicPattern, _newNotificationRequests);
            else
                _repository.RemoveRequest(notifiable, notificationRequest.TopicPattern);
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
