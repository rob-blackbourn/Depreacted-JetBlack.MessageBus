using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Messages;
using BufferManager = System.ServiceModel.Channels.BufferManager;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class Interactor : IDisposable, IEquatable<Interactor>, IComparable<Interactor>
    {
        public readonly int Id;

        private readonly Socket _socket;
        private readonly BufferManager _bufferManager;
        private readonly IObserver<Message> _messageObserver;

        public Interactor(Socket socket, int id, bool isAuthenticationRequired, BufferManager bufferManager, CancellationToken token)
        {
            _socket = socket;
            Id = id;
            AuthenticationStatus = isAuthenticationRequired ? AuthenticationStatus.Required : AuthenticationStatus.None;
            _bufferManager = bufferManager;
            _messageObserver = socket.ToMessageObserver(_bufferManager, token);
        }

        public IObservable<Message> ToObservable()
        {
            return _socket.ToMessageObservable(_bufferManager);
        }

        public void SendMessage(Message message)
        {
            _messageObserver.OnNext(message);
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint) _socket.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint) _socket.RemoteEndPoint; }
        }

        public Socket Socket
        {
            get { return _socket; }
        }

        public AuthenticationStatus AuthenticationStatus { get; set; }

        public byte[] Identity { get; set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Id, RemoteEndPoint);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Interactor);
        }

        public bool Equals(Interactor other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(Interactor other)
        {
            return (other == null ? 1 : Id - other.Id);
        }

        public static bool operator ==(Interactor a, Interactor b)
        {
            return (ReferenceEquals(a,null) && ReferenceEquals(b, null)) || (!ReferenceEquals(a, null) && a.Equals(b));
        }

        public static bool operator !=(Interactor a, Interactor b)
        {
            return !(a == b);
        }

        public void Dispose()
        {
            _socket.Close();
        }
    }
}
