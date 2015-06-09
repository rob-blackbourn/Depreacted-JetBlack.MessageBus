using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class InteractorManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISet<Interactor> _interactors = new HashSet<Interactor>();
        private readonly ISubject<Interactor> _closedInteractors = new Subject<Interactor>();
        private readonly ISubject<SourceMessage<Exception>> _faultedInteractors = new Subject<SourceMessage<Exception>>();

        public InteractorManager()
        {
            var scheduler = new EventLoopScheduler();

            _closedInteractors.ObserveOn(scheduler).Subscribe(RemoveInteractor);
            _faultedInteractors.ObserveOn(scheduler).Subscribe(FaultedInteractor);
        }

        public IObservable<Interactor> ClosedInteractors
        {
            get { return _closedInteractors; }
        }

        public IObservable<SourceMessage<Exception>> FaultedInteractors
        {
            get { return _faultedInteractors; }
        }

        public void AddInteractor(Interactor interactor)
        {
            Log.DebugFormat("Adding interactor: {0}", interactor);

            _interactors.Add(interactor);
        }

        public void RemoveInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing interactor: {0}", interactor);

            _interactors.Remove(interactor);
        }

        public void CloseInteractor(Interactor interactor)
        {
            Log.DebugFormat("Closing interactor: {0}", interactor);

            _closedInteractors.OnNext(interactor);
        }

        public void FaultInteractor(Interactor interactor, Exception error)
        {
            Log.DebugFormat("Faulting interactor: {0}", interactor);

            _faultedInteractors.OnNext(new SourceMessage<Exception>(interactor, error));
        }

        private void FaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);

            RemoveInteractor(sourceMessage.Source);
        }

        public void Dispose()
        {
            Log.DebugFormat("Disposing");

            foreach (var interactor in _interactors)
                interactor.Dispose();
        }
    }
}
