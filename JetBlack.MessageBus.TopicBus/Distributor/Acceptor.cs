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

        public Acceptor(int maxBufferPoolSize, int maxBufferSize)
        {
            _bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
        }

        public IObservable<Interactor> ToObservable(IPEndPoint endPoint, CancellationToken token)
        {
            return Observable.Create<Interactor>(observer =>
                endPoint.ToListenerObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(socket => observer.OnNext(new Interactor(socket, _nextId++, _bufferManager, token))));
        }
    }
}
