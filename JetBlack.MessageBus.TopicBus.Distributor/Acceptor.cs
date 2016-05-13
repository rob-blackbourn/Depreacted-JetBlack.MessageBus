using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using log4net;
using JetBlack.MessageBus.Common.Network;

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

        public IObservable<IInteractor> ToObservable(IPEndPoint endpoint)
        {
            return Observable.Create<IInteractor>(observer =>
                endpoint.ToListenerAsyncObservable(10)
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Subscribe(async tcpClient => await Accept(tcpClient, observer)));
        }

        private async Task Accept(TcpClient tcpClient, IObserver<IInteractor> observer)
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
        }
    }
}
