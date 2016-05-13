using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using log4net;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class SubscriptionMarshaller : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly SubscriptionManager _manager;

        private readonly ISubject<SourceMessage<SubscriptionRequest>> _subscriptionRequests = new Subject<SourceMessage<SubscriptionRequest>>();
        private readonly ISubject<SourceMessage<MulticastData>> _publishedMulticastDataMessages = new Subject<SourceMessage<MulticastData>>();
        private readonly ISubject<SourceMessage<UnicastData>> _publishedUnicastDataMessages = new Subject<SourceMessage<UnicastData>>();
        private readonly IDisposable _disposable;

        public SubscriptionMarshaller(InteractorManager interactorManager, NotificationMarshaller notificationMarshaller, PublisherMarshaller publisherMarshaller)
        {
            _manager = new SubscriptionManager(notificationMarshaller, publisherMarshaller);

            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _subscriptionRequests.ObserveOn(scheduler).Subscribe(x => _manager.RequestSubscription(x.Source, x.Content)),
                    _publishedMulticastDataMessages.ObserveOn(scheduler).Subscribe(x => _manager.SendMulticastData(x.Source, x.Content)),
                    _publishedUnicastDataMessages.ObserveOn(scheduler).Subscribe(x => _manager.SendUnicastData(x.Source, x.Content)),

                    notificationMarshaller.NewNotificationRequests.ObserveOn(scheduler).Subscribe(x => _manager.OnNewNotificationRequest(x.Source, x.Content)),

                    publisherMarshaller.StalePublishers.ObserveOn(scheduler).Subscribe(x => _manager.OnStaleTopics(x.Content)),

                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(_manager.OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => _manager.OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public void SendMulticastData(IInteractor sender, MulticastData multicastData)
        {
            Log.DebugFormat("SendMulticastData(sender={0}, multicastData={1})", sender, multicastData);

            _publishedMulticastDataMessages.OnNext(SourceMessage.Create(sender, multicastData));
        }

        public void SendUnicastData(IInteractor sender, UnicastData unicastData)
        {
            Log.DebugFormat("SendUnicastData(sender={0}, unicastData={1})", sender, unicastData);

            _publishedUnicastDataMessages.OnNext(SourceMessage.Create(sender, unicastData));
        }

        public void RequestSubscription(IInteractor subscriber, SubscriptionRequest subscriptionRequest)
        {
            Log.DebugFormat("RequestSubscription(sender={0}, request={1})", subscriber, subscriptionRequest);

            _subscriptionRequests.OnNext(SourceMessage.Create(subscriber, subscriptionRequest));
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
