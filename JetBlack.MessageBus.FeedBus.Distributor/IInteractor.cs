using System;
using System.Net;
using JetBlack.MessageBus.FeedBus.Messages;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal interface IInteractor : IDisposable, IEquatable<IInteractor>, IComparable<IInteractor>
    {
        int Id { get; }
        string Name { get; }
        IPAddress IPAddress { get; }

        IObservable<Message> ToObservable();
        void SendMessage(Message message);
    }
}
