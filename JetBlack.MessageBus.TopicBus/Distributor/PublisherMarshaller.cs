using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherMarshaller : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly PublisherManager _publisherManager = new PublisherManager();

        private readonly ISubject<SourceSinkMessage<MulticastData>> _sendableMulticastDataMessages = new Subject<SourceSinkMessage<MulticastData>>();
        private readonly ISubject<SourceSinkMessage<UnicastData>> _sendableUnicastDataMessages = new Subject<SourceSinkMessage<UnicastData>>();

        private readonly IDisposable _disposable;

        public IObservable<SourceMessage<IEnumerable<string>>> StalePublishers
        {
            get { return _publisherManager.StalePublishers; }
        }

        public PublisherMarshaller(InteractorManager interactorManager)
        {
            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _sendableMulticastDataMessages.ObserveOn(scheduler).Subscribe(x => _publisherManager.SendMulticastData(x.Source, x.Content, x.Sink)),
                    _sendableUnicastDataMessages.ObserveOn(scheduler).Subscribe(x => _publisherManager.SendUnicastData(x.Source, x.Content, x.Sink)),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(_publisherManager.OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => _publisherManager.OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public void SendUnicastData(Interactor publisher, Interactor subscriber, UnicastData unicastData)
        {
            _sendableUnicastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, unicastData));
        }

        public void SendMulticastData(Interactor publisher, IEnumerable<Interactor> subscribers, MulticastData multicastData)
        {
            foreach (var subscriber in subscribers)
                _sendableMulticastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, multicastData));
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
