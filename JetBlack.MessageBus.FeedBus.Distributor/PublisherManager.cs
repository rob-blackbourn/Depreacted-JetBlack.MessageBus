using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using log4net;
using JetBlack.MessageBus.FeedBus.Messages;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class PublisherManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly PublisherRepository _repository = new PublisherRepository();
        private readonly ISubject<SourceMessage<IEnumerable<FeedAndTopic>>> _stalePublishers = new Subject<SourceMessage<IEnumerable<FeedAndTopic>>>();

        // TODO: This should just be by feed.
        public IObservable<SourceMessage<IEnumerable<FeedAndTopic>>> StalePublishers
        {
            get { return _stalePublishers; }
        }

        public void SendMulticastData(IInteractor publisher, MulticastData multicastData, IInteractor subscriber)
        {
            _repository.AddPublisher(publisher, multicastData.Feed, multicastData.Topic);
            subscriber.SendMessage(multicastData);
        }

        public void SendUnicastData(IInteractor publisher, UnicastData unicastData, IInteractor subscriber)
        {
            _repository.AddPublisher(publisher, unicastData.Feed, unicastData.Topic);
            subscriber.SendMessage(unicastData);
        }

        public void OnClosedInteractor(IInteractor interactor)
        {
            var feedsAndTopicsWithoutPublishers = _repository.RemovePublisher(interactor);
            if (feedsAndTopicsWithoutPublishers != null)
                _stalePublishers.OnNext(SourceMessage.Create(interactor, feedsAndTopicsWithoutPublishers));
        }

        public void OnFaultedInteractor(IInteractor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }
    }
}
