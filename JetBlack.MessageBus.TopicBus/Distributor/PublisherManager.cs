using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBlack.MessageBus.Common.Collections;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TwoWaySet<string, Interactor> _topicsAndPublishers = new TwoWaySet<string, Interactor>();

        private readonly ISubject<SourceSinkMessage<MulticastData>> _sendableMulticastDataMessages = new Subject<SourceSinkMessage<MulticastData>>();
        private readonly ISubject<SourceSinkMessage<UnicastData>> _sendableUnicastDataMessages = new Subject<SourceSinkMessage<UnicastData>>();
        private readonly ISubject<SourceMessage<IEnumerable<string>>> _stalePublishers = new Subject<SourceMessage<IEnumerable<string>>>();

        private readonly IDisposable _disposable;

        public IObservable<SourceMessage<IEnumerable<string>>> StalePublishers
        {
            get { return _stalePublishers; }
        }

        public PublisherManager(InteractorManager interactorManager)
        {
            var scheduler = new EventLoopScheduler();

            _disposable = new CompositeDisposable(
                new[]
                {
                    _sendableMulticastDataMessages.ObserveOn(scheduler).Subscribe(x => OnMulticastMessage(x.Source, x.Content, x.Sink)),
                    _sendableUnicastDataMessages.ObserveOn(scheduler).Subscribe(x => OnUnicastMessage(x.Source, x.Content, x.Sink)),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(x => OnFaultedInteractor(x.Source, x.Content))
                });
        }

        public void Send(Interactor publisher, Interactor subscriber, UnicastData unicastData)
        {
            _sendableUnicastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, unicastData));
        }

        public void Send(Interactor publisher, IEnumerable<Interactor> subscribers, MulticastData multicastData)
        {
            foreach (var subscriber in subscribers)
                _sendableMulticastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, multicastData));
        }

        private void OnMulticastMessage(Interactor publisher, MulticastData multicastData, Interactor subscriber)
        {
            _topicsAndPublishers.Add(publisher, multicastData.Topic);
            subscriber.SendMessage(multicastData);
        }

        private void OnUnicastMessage(Interactor publisher, UnicastData unicastData, Interactor subscriber)
        {
            _topicsAndPublishers.Add(publisher, unicastData.Topic);
            subscriber.SendMessage(unicastData);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            var topicsWithoutPublishers = _topicsAndPublishers.Remove(interactor);
            if (topicsWithoutPublishers != null)
                _stalePublishers.OnNext(SourceMessage.Create(interactor, topicsWithoutPublishers));
        }

        private void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
