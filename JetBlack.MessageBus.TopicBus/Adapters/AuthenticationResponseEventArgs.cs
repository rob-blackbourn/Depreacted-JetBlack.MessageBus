using System;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class AuthenticationResponseEventArgs<TData> : EventArgs
    {
        public AuthenticationResponseEventArgs(AuthenticationStatus status, TData data)
        {
            Status = status;
            Data = data;
        }

        public AuthenticationStatus Status { get; private set; }
        public TData Data { get; private set; }
    }
}
