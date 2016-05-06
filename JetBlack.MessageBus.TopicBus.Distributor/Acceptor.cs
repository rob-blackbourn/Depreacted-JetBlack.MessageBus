using System;
using System.Net;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using JetBlack.MessageBus.Common.Network;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class Acceptor
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                    .Subscribe(async tcpClient =>
                    {
                        try
                        {
                            var interactor = await Interactor.Create(tcpClient, _nextId++, _bufferManager);
                            observer.OnNext(interactor);
                        }
                        catch (Exception error)
                        {
                            Log.Warn("Failed to create interactor", error);
                        }
                    }));
        }
    }
}
