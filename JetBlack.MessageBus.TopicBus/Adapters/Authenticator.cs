using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Distributor;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public interface IAuthenticationProvider
    {
        IObserver<ForwardedAuthenticationRequest> RequestObserver { get; }
        IObservable<AuthenticationResponse> ToResponseObservable(int clientId);
    }

    public class Authenticator : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly IDisposable _acceptorDisposable;

        protected Authenticator(Socket socket, IAuthenticationProvider authenticationProvider, BufferManager bufferManager, IScheduler scheduler, CancellationToken token)
        {
            var acceptor = new Acceptor(socket, bufferManager);
            _acceptorDisposable = acceptor.ToObservable(false, token)
                .ObserveOn(scheduler)
                .Subscribe(client => new ClientAuthenticator(client, authenticationProvider), OnAcceptorError, OnAcceptorCompleted);
        }

        private void OnAcceptorError(Exception error)
        {
            Log.Error("The acceptor has faulted.", error);
        }

        private void OnAcceptorCompleted()
        {
        }

        public void Dispose()
        {
            _acceptorDisposable.Dispose();
        }

        class ClientAuthenticator
        {
            private readonly Interactor _client;
            private readonly IDisposable _clientDisposable;
            private readonly IAuthenticationProvider _authenticationProvider;
            private readonly IDisposable _authenticationProviderDisposable;

            public ClientAuthenticator(Interactor client, IAuthenticationProvider authenticationProvider)
            {
                var scheduler = new EventLoopScheduler();

                _client = client;
                _authenticationProvider = authenticationProvider;

                _clientDisposable = _client.ToObservable()
                    .ObserveOn(scheduler)
                    .Subscribe(OnAuthenticatingClientNext, OnAuthenticatingClientError, OnAuthenticatingClientCompleted);

                _authenticationProviderDisposable = authenticationProvider.ToResponseObservable(_client.Id)
                    .ObserveOn(scheduler)
                    .Subscribe(OnAuthenticationProviderNext, OnAuthenticationProviderError, OnAuthenticationProviderCompleted);
            }

            private void OnAuthenticatingClientNext(Message message)
            {
                switch (message.MessageType)
                {
                    case MessageType.AuthenticationRequest:
                        _authenticationProvider.RequestObserver.OnNext((ForwardedAuthenticationRequest) message);
                        break;

                    default:
                        Log.WarnFormat("Invalid message received: {0}", message.MessageType);
                        _client.Dispose();
                        break;
                }
            }

            private void OnAuthenticatingClientError(Exception error)
            {
                Log.Warn("The client has faulted.", error);
                OnAuthenticatingClientCompleted();
            }

            private void OnAuthenticatingClientCompleted()
            {
                _authenticationProviderDisposable.Dispose();
            }

            private void OnAuthenticationProviderNext(AuthenticationResponse authenticationResponse)
            {
                _client.SendMessage(new ForwardedAuthenticationResponse(authenticationResponse.Status, authenticationResponse.Data));
            }

            private void OnAuthenticationProviderError(Exception error)
            {
                Log.Warn("The authentication provider has faulted.", error);
                OnAuthenticationProviderCompleted();
            }

            private void OnAuthenticationProviderCompleted()
            {
                _clientDisposable.Dispose();
            }
        }
    }
}
