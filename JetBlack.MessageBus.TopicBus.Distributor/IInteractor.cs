using System;
using System.Net;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal interface IInteractor : IDisposable, IEquatable<IInteractor>, IComparable<IInteractor>
    {
        int Id { get; }
        string Name { get; }
        IPEndPoint LocalEndPoint { get; }
        IPEndPoint RemoteEndPoint { get; }

        IObservable<Message> ToObservable();
        void SendMessage(Message message);
    }
}
