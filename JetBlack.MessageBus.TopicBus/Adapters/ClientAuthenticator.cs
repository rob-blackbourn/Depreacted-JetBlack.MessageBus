using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBlack.MessageBus.TopicBus.Distributor;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    internal class ClientAuthenticator
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Interactor _client;
        private readonly IDisposable _clientDisposable;
        private readonly IAuthenticationProvider _authenticationProvider;
        private readonly IDisposable _authenticationProviderDisposable;

        public ClientAuthenticator(Interactor client, IAuthenticationProvider authenticationProvider)
        {
            _client = client;
            _authenticationProvider = authenticationProvider;

            var scheduler = new EventLoopScheduler();

            _clientDisposable = _client.ToObservable()
                .ObserveOn(scheduler)
                .Subscribe(OnAuthenticatingClientNext, OnAuthenticatingClientError, OnAuthenticatingClientCompleted);

            _authenticationProviderDisposable = _authenticationProvider.ToResponseObservable(_client.Id)
                .ObserveOn(scheduler)
                .Subscribe(OnAuthenticationProviderNext, OnAuthenticationProviderError, OnAuthenticationProviderCompleted);
        }

        private void OnAuthenticatingClientNext(Message message)
        {
            switch (message.MessageType)
            {
                case MessageType.AuthenticationRequest:
                    RequestAuthentication((AuthenticationRequest)message);
                    break;

                case MessageType.ForwardedAuthenticationRequest:
                    RequestAuthentication((ForwardedAuthenticationRequest)message);
                    break;

                default:
                    Log.WarnFormat("Invalid message received: {0}", message.MessageType);
                    _client.Dispose();
                    break;
            }
        }

        private void RequestAuthentication(AuthenticationRequest authenticationRequest)
        {
            RequestAuthentication(new ForwardedAuthenticationRequest(_client.Id, authenticationRequest.Data));
        }

        private void RequestAuthentication(ForwardedAuthenticationRequest forwardedAuthenticationRequest)
        {
            _authenticationProvider.RequestAuthentication(forwardedAuthenticationRequest);
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