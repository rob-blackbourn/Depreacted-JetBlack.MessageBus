using System;
using System.Net;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Distributor.Test.Mocks
{
    internal class MockInteractor : IInteractor
    {
        public MockInteractor(int id, string name, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Id = id;
            Name = name;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public IPEndPoint LocalEndPoint { get; private set; }
        public IPEndPoint RemoteEndPoint { get; private set; }

        public bool Equals(IInteractor other)
        {
            return other != null && Id == other.Id;
        }

        public int CompareTo(IInteractor other)
        {
            return other.Id - Id;
        }

        public void Dispose()
        {
        }

        public void SendMessage(Message message)
        {
            throw new NotImplementedException();
        }

        public IObservable<Message> ToObservable()
        {
            throw new NotImplementedException();
        }
    }
}
