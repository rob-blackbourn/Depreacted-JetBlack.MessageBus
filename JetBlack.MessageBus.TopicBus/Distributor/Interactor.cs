using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;
using BufferManager = System.ServiceModel.Channels.BufferManager;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    class Interactor : IDisposable, IEquatable<Interactor>, IComparable<Interactor>
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly TcpClient _tcpClient;
        public readonly int Id;
        readonly BufferManager _bufferManager;
        private readonly IObserver<Message> _messageObserver;

        public Interactor(TcpClient tcpClient, int id, BufferManager bufferManager, CancellationToken token)
        {
            _tcpClient = tcpClient;
            Id = id;
            _bufferManager = bufferManager;
            _messageObserver = tcpClient.ToMessageObserver(_bufferManager, token);
        }

        public IObservable<Message> ToObservable()
        {
            return _tcpClient.ToMessageObservable(_bufferManager);
        }

        public void SendMessage(Message message)
        {
            Log.DebugFormat("Sending {0} ({1})", this, message);
            _messageObserver.OnNext(message);
        }

        public IPEndPoint LocalEndPoint
        {
            get { return (IPEndPoint)_tcpClient.Client.LocalEndPoint; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return (IPEndPoint)_tcpClient.Client.RemoteEndPoint; }
        }

        public Socket Socket
        {
            get { return _tcpClient.Client; }
        }

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

        public void Dispose()
        {
            _tcpClient.Close();
        }
    }
}
