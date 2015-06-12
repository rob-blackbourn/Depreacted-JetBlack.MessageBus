using System;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public interface IAuthenticationProvider
    {
        void RequestAuthentication(ForwardedAuthenticationRequest forwardedAuthenticationRequest);
        IObservable<AuthenticationResponse> ToResponseObservable(int clientId);
    }
}