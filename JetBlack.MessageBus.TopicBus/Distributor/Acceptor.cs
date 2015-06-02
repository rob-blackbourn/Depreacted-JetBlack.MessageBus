using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.Common.Network;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    class Acceptor
    {
        const int MaxBufferPoolSize = 100;
        const int MaxBufferSize = 100000;

        int _nextInteractorId;
        readonly BufferManager _bufferManager;

        public Acceptor()
        {
            _bufferManager = BufferManager.CreateBufferManager(MaxBufferPoolSize, MaxBufferSize);
        }

        public IObservable<Interactor> ToObservable(IPEndPoint endPoint, CancellationToken token)
        {
            return Observable.Create<Interactor>(observer =>
                endPoint.ToListenerObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(client => observer.OnNext(new Interactor(client, _nextInteractorId++, _bufferManager, token))));
        }
    }
}
