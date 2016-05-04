using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using JetBlack.MessageBus.Common.Collections;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherManager
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TwoWaySet<string, Interactor> _topicsAndPublishers = new TwoWaySet<string, Interactor>();
        private readonly ISubject<SourceMessage<IEnumerable<string>>> _stalePublishers = new Subject<SourceMessage<IEnumerable<string>>>();

        public IObservable<SourceMessage<IEnumerable<string>>> StalePublishers
        {
            get { return _stalePublishers; }
        }

        public void SendMulticastData(Interactor publisher, MulticastData multicastData, Interactor subscriber)
        {
            _topicsAndPublishers.Add(publisher, multicastData.Topic);
            subscriber.SendMessage(multicastData);
        }

        public void SendUnicastData(Interactor publisher, UnicastData unicastData, Interactor subscriber)
        {
            _topicsAndPublishers.Add(publisher, unicastData.Topic);
            subscriber.SendMessage(unicastData);
        }

        public void OnClosedInteractor(Interactor interactor)
        {
            var topicsWithoutPublishers = _topicsAndPublishers.Remove(interactor);
            if (topicsWithoutPublishers != null)
                _stalePublishers.OnNext(SourceMessage.Create(interactor, topicsWithoutPublishers));
        }

        public void OnFaultedInteractor(Interactor interactor, Exception error)
        {
            Log.Warn("Interactor faulted: " + interactor, error);
            OnClosedInteractor(interactor);
        }
    }
}
