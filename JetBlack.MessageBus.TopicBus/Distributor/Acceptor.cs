using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.Common.Network;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class Acceptor
    {
        private int _nextId;
        private readonly Socket _socket;
        private readonly BufferManager _bufferManager;

        public Acceptor(IPEndPoint endPoint, BufferManager bufferManager)
            : this(CreateAndBind(endPoint), bufferManager)
        {
        }

        public Acceptor(Socket socket, BufferManager bufferManager)
        {
            _socket = socket;
            _bufferManager = bufferManager;
        }

        public IObservable<Interactor> ToObservable(CancellationToken token)
        {
            return Observable.Create<Interactor>(observer =>
                _socket.ToListenerObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(client => observer.OnNext(new Interactor(client, _nextId++, _bufferManager, token))));
        }

        private static Socket CreateAndBind(EndPoint endpoint)
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind(endpoint);
            return socket;
        }
    }
}
