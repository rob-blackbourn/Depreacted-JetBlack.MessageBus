using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using log4net;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly PublisherRepository _repository = new PublisherRepository();
        private readonly ISubject<SourceMessage<IEnumerable<string>>> _stalePublishers = new Subject<SourceMessage<IEnumerable<string>>>();

        public IObservable<SourceMessage<IEnumerable<string>>> StalePublishers
        {
            get { return _stalePublishers; }
        }

        public void SendMulticastData(IInteractor publisher, MulticastData multicastData, IInteractor subscriber)
        {
            _repository.AddPublisher(publisher, multicastData.Topic);
            subscriber.SendMessage(multicastData);
        }

        public void SendUnicastData(IInteractor publisher, UnicastData unicastData, IInteractor subscriber)
        {
            _repository.AddPublisher(publisher, unicastData.Topic);
            subscriber.SendMessage(unicastData);
        }

        public void OnClosedInteractor(IInteractor interactor)
        {
            var topicsWithoutPublishers = _repository.RemovePublisher(interactor);
            if (topicsWithoutPublishers != null)
                _stalePublishers.OnNext(SourceMessage.Create(interactor, topicsWithoutPublishers));
        }

        public void OnFaultedInteractor(IInteractor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }
    }
}
