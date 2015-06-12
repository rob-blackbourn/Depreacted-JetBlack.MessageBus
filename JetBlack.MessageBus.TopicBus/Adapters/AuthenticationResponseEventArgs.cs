using System;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class AuthenticationResponseEventArgs<T> : EventArgs
    {
        public AuthenticationResponseEventArgs(AuthenticationStatus status, T data)
        {
            Status = status;
            Data = data;
        }

        public AuthenticationStatus Status { get; private set; }
        public T Data { get; private set; }
    }
}
