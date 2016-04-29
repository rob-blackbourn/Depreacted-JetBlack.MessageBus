using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;
using Message = JetBlack.MessageBus.TopicBus.Messages.Message;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class Market : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDisposable _listenerDisposable;
        private readonly InteractorManager _interactorManager;
        private readonly SubscriptionMarshaller _subscriptionMarshaller;
        private readonly PublisherMarshaller _publisherMarshaller;
        private readonly NotificationMarshaller _notificationMarshaller;
        private readonly IScheduler _scheduler;

        public Market(IObservable<Interactor> listenerObservable)
        {
            _interactorManager = new InteractorManager();
            _notificationMarshaller = new NotificationMarshaller(_interactorManager);
            _publisherMarshaller = new PublisherMarshaller(_interactorManager);
            _subscriptionMarshaller = new SubscriptionMarshaller(_interactorManager, _notificationMarshaller, _publisherMarshaller);

            _scheduler = new EventLoopScheduler();

            _listenerDisposable = listenerObservable
                .ObserveOn(_scheduler)
                .Subscribe(AddInteractor);
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
                    _subscriptionMarshaller.RequestSubscription(sender, (SubscriptionRequest)message);
                    break;

                case MessageType.MulticastData:
                    _subscriptionMarshaller.SendMulticastData(sender, (MulticastData)message);
                    break;

                case MessageType.UnicastData:
                    _subscriptionMarshaller.SendUnicastData(sender, (UnicastData)message);
                    break;

                case MessageType.NotificationRequest:
                    _notificationMarshaller.RequestNotification(sender, (NotificationRequest)message);
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
            _subscriptionMarshaller.Dispose();
            _publisherMarshaller.Dispose();
            _notificationMarshaller.Dispose();
        }
    }
}
