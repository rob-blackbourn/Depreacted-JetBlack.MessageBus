using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class Market : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDisposable _listenerDisposable;
        private readonly InteractorManager _interactorManager;
        private readonly SubscriptionManager _subscriptionManager;
        private readonly PublisherManager _publisherManager;
        private readonly NotificationManager _notificationManager;
        private readonly IScheduler _scheduler;

        public Market(IObservable<Interactor> listenerObservable)
        {
            _interactorManager = new InteractorManager();
            _notificationManager = new NotificationManager(_interactorManager);
            _publisherManager = new PublisherManager(_interactorManager);
            _subscriptionManager = new SubscriptionManager(_interactorManager, _notificationManager, _publisherManager);

            _scheduler = new EventLoopScheduler();

            _listenerDisposable = listenerObservable.ObserveOn(_scheduler).Subscribe(AddInteractor);
        }

        private void AddInteractor(Interactor interactor)
        {
            Log.DebugFormat("AddInteractor(interactor={0})", interactor);

            _interactorManager.AddInteractor(interactor);

            interactor.ToObservable()
                .ObserveOn(_scheduler)
                .Subscribe(
                    message => OnMessage(interactor, message),
                    error => _interactorManager.FaultInteractor(interactor, error),
                    () => _interactorManager.CloseInteractor(interactor));
        }

        private void OnMessage(Interactor sender, Message message)
        {
            Log.DebugFormat("OnMessage(sender={0}, message={1}", sender, message);

            switch (message.MessageType)
            {
                case MessageType.SubscriptionRequest:
                    _subscriptionManager.RequestSubscription(sender, (SubscriptionRequest)message);
                    break;

                case MessageType.MulticastData:
                    _subscriptionManager.SendMulticastData(sender, (MulticastData) message);
                    break;

                case MessageType.UnicastData:
                    _subscriptionManager.SendUnicastData(sender, (UnicastData) message);
                    break;

                case MessageType.NotificationRequest:
                    _notificationManager.RequestNotification(sender, (NotificationRequest) message);
                    break;

                default:
                    throw new ArgumentException("invalid message type");
            }
        }

        public void Dispose()
        {
            Log.DebugFormat("Dispose");

            _listenerDisposable.Dispose();

            _interactorManager.Dispose();
            _subscriptionManager.Dispose();
            _publisherManager.Dispose();
            _notificationManager.Dispose();
        }
    }
}
