using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class InteractorManager : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly ISubject<ForwardedAuthenticationRequest,AuthenticationResponse> _authenticator;
        private readonly IDisposable _authenticatorDisposable;
        private readonly IDictionary<int, Interactor> _interactors = new Dictionary<int, Interactor>();
        private readonly ISubject<Interactor> _closedInteractors = new Subject<Interactor>();
        private readonly ISubject<SourceMessage<Exception>> _faultedInteractors = new Subject<SourceMessage<Exception>>();

        public InteractorManager(ISubject<ForwardedAuthenticationRequest,AuthenticationResponse> authenticator)
        {
            var scheduler = new EventLoopScheduler();

            _closedInteractors.ObserveOn(scheduler).Subscribe(RemoveInteractor);
            _faultedInteractors.ObserveOn(scheduler).Subscribe(FaultedInteractor);

            _authenticator = authenticator;

            _authenticatorDisposable = _authenticator
                .ObserveOn(scheduler)
                .Subscribe(OnAuthenticatorResponse, OnAuthenticatorError, OnAuthenticatorCompleted);
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

            _interactors.Add(interactor.Id, interactor);
        }

        public void RemoveInteractor(Interactor interactor)
        {
            Log.DebugFormat("Removing interactor: {0}", interactor);

            _interactors.Remove(interactor.Id);
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

        public void OnAuthenticationRequest(Interactor client, AuthenticationRequest authenticationRequest)
        {
            client.Status = AuthenticationStatus.Requested;
            _authenticator.OnNext(new ForwardedAuthenticationRequest(client.Id, authenticationRequest.Data));
        }

        private void OnAuthenticatorError(Exception error)
        {
            Log.Error("The authenticator has faulted.", error);
        }

        private void OnAuthenticatorCompleted()
        {
            Log.InfoFormat("The authenticator has completed.");
        }

        private void OnAuthenticatorResponse(AuthenticationResponse authenticationResponse)
        {
            Interactor client;
            if (!_interactors.TryGetValue(authenticationResponse.ClientId, out client))
                return;

            switch (authenticationResponse.Status)
            {
                case AuthenticationStatus.Requested:
                    client.SendMessage(authenticationResponse);
                    break;
                case AuthenticationStatus.Accepted:
                    client.Status = AuthenticationStatus.Accepted;
                    client.Identity = authenticationResponse.Data;
                    client.SendMessage(new ForwardedAuthenticationResponse(authenticationResponse.Status, authenticationResponse.Data));
                    break;
                case AuthenticationStatus.Rejected:
                    client.Status = AuthenticationStatus.Rejected;
                    client.SendMessage(new ForwardedAuthenticationResponse(authenticationResponse.Status, authenticationResponse.Data));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void FaultedInteractor(SourceMessage<Exception> sourceMessage)
        {
            Log.Warn("Interactor faulted: " + sourceMessage.Source, sourceMessage.Content);

            RemoveInteractor(sourceMessage.Source);
        }

        public void Dispose()
        {
            Log.DebugFormat("Disposing");

            _authenticatorDisposable.Dispose();

            foreach (var interactor in _interactors.Values)
                interactor.Dispose();
        }
    }
}
