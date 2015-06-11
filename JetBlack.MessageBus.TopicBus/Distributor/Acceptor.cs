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

        public IObservable<Interactor> ToObservable(IPEndPoint endPoint, bool isAuthenticationRequired, CancellationToken token)
        {
            return Observable.Create<Interactor>(observer =>
                endPoint.ToListenerObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(socket => observer.OnNext(new Interactor(socket, _nextId++, isAuthenticationRequired, _bufferManager, token))));
        }
    }
}
