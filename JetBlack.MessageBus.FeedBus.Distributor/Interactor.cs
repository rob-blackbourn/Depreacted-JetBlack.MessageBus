using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading.Tasks;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using JetBlack.MessageBus.FeedBus.Messages;
using JetBlack.MessageBus.FeedBus.Distributor.Config;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class Interactor : IInteractor
    {
        private readonly Stream _stream;
        private readonly BufferManager _bufferManager;
        private readonly IObserver<Message> _messageObserver;
        private readonly PermissionManager _permissionManager;

        public static async Task<Interactor> Create(TcpClient tcpClient, int id, BufferManager bufferManager, DistributorConfig config)
        {
            var stream = new NegotiateStream(tcpClient.GetStream());
            await stream.AuthenticateAsServerAsync();
            return new Interactor(stream, id, stream.RemoteIdentity.Name, ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address, bufferManager, config);
        }

        private Interactor(Stream stream, int id, string name, IPAddress ipAddress, BufferManager bufferManager, DistributorConfig config)
        {
            _stream = stream;
            Id = id;
            _bufferManager = bufferManager;

            Name = name;
            IPAddress = ipAddress;

            _permissionManager = new PermissionManager(config, ipAddress, name);

            _messageObserver = stream.ToMessageObserver(_bufferManager);
        }

        public IObservable<Message> ToObservable()
        {
            return _stream.ToMessageObservable(_bufferManager);
        }

        public bool HasRole(string feed, ClientRole role)
        {
            return _permissionManager.HasRole(feed, role);
        }

        public void SendMessage(Message message)
        {
            _messageObserver.OnNext(message);
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public IPAddress IPAddress { get; private set; }

        public override string ToString()
        {
            return string.Format("{0}/{1}", Id, IPAddress);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Interactor);
        }

        public bool Equals(IInteractor other)
        {
            return other != null && other.Id == Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public int CompareTo(IInteractor other)
        {
            return (other == null ? 1 : Id - other.Id);
        }

        public static bool operator ==(Interactor a, Interactor b)
        {
            return (ReferenceEquals(a, null) && ReferenceEquals(b, null)) || (!ReferenceEquals(a, null) && a.Equals(b));
        }

        public static bool operator !=(Interactor a, Interactor b)
        {
            return !(a == b);
        }

        public void Dispose()
        {
            _stream.Close();
        }
    }
}
