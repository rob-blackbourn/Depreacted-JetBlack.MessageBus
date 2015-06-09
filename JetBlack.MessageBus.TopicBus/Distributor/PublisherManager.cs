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
    internal class PublisherManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Dictionary<string, ISet<Interactor>> _topicToPublisherMap = new Dictionary<string, ISet<Interactor>>();
        private readonly Dictionary<Interactor, ISet<string>> _publisherToTopicMap = new Dictionary<Interactor, ISet<string>>();
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
                    _sendableMulticastDataMessages.ObserveOn(scheduler).Subscribe(OnMulticastMessage),
                    _sendableUnicastDataMessages.ObserveOn(scheduler).Subscribe(OnUnicastMessage),
                    interactorManager.ClosedInteractors.ObserveOn(scheduler).Subscribe(OnClosedInteractor),
                    interactorManager.FaultedInteractors.ObserveOn(scheduler).Subscribe(OnFaultedInteractor)
                });
        }

        public void Send(Interactor publisher, Interactor subscriber, UnicastData unicastData)
        {
            _sendableUnicastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, unicastData));
        }

        public void Send(Interactor publisher, Interactor subscriber, MulticastData multicastData)
        {
            _sendableMulticastDataMessages.OnNext(SourceSinkMessage.Create(publisher, subscriber, multicastData));
        }

        private void OnMulticastMessage(SourceSinkMessage<MulticastData> routedMessage)
        {
            RememberPublisherForTopic(routedMessage.Content.Topic, routedMessage.Source);
            routedMessage.Sink.SendMessage(routedMessage.Content);
        }

        private void OnUnicastMessage(SourceSinkMessage<UnicastData> routedMessage)
        {
            RememberPublisherForTopic(routedMessage.Content.Topic, routedMessage.Source);
            routedMessage.Sink.SendMessage(routedMessage.Content);
        }

        private void RememberPublisherForTopic(string topic, Interactor publisher)
        {
            ISet<Interactor> publishers;
            if (!_topicToPublisherMap.TryGetValue(topic, out publishers))
                _topicToPublisherMap.Add(topic, publishers = new HashSet<Interactor>());
            if (!publishers.Contains(publisher))
                publishers.Add(publisher);

            ISet<string> topics;
            if (!_publisherToTopicMap.TryGetValue(publisher, out topics))
                _publisherToTopicMap.Add(publisher, topics = new HashSet<string>());
            if (!topics.Contains(topic))
                topics.Add(topic);
        }

        private void OnClosedInteractor(Interactor interactor)
        {
            var staleTopics = new HashSet<string>();

            ISet<string> topics;
            if (_publisherToTopicMap.TryGetValue(interactor, out topics))
            {
                foreach (var topic in topics)
                {
                    ISet<Interactor> publishers;
                    if (_topicToPublisherMap.TryGetValue(topic, out publishers) && publishers.Contains(interactor))
                    {
                        publishers.Remove(interactor);
                        if (publishers.Count == 0)
                        {
                            _topicToPublisherMap.Remove(topic);
                            staleTopics.Add(topic);
                        }
                    }
                }

                _publisherToTopicMap.Remove(interactor);
            }

            _stalePublishers.OnNext(SourceMessage.Create<IEnumerable<string>>(interactor, staleTopics));
        }

        private void OnFaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);
            OnClosedInteractor(sourceMessage.Source);
        }

        public void Dispose()
        {
            _disposable.Dispose();
        }
    }
}
