using System;
using System.Net;
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
        private readonly BufferManager _bufferManager;

        public Acceptor(BufferManager bufferManager)
        {
            _bufferManager = bufferManager;
        }

        public IObservable<Interactor> ToObservable(IPEndPoint endpoint)
        {
            return Observable.Create<Interactor>(observer =>
                endpoint.ToListenerAsyncObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(tcpClient => observer.OnNext(new Interactor(tcpClient, _nextId++, _bufferManager))));
        }
    }
}
