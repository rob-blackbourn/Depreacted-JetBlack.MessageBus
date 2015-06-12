using System;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.Common.Network;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Market _market;

        public Server(IPEndPoint serverEndPoint, IPEndPoint authenticatorEndpoint, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            Log.Info("Starting server");

            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            var acceptor = new Acceptor(serverEndPoint, bufferManager);

            Market market = null;

            if (authenticatorEndpoint == null)
                market = new Market(acceptor.ToObservable(false, token), null);
            else
                authenticatorEndpoint.ToConnectObservable()
                    .Subscribe(
                        socket => market = new Market(acceptor.ToObservable(true, token), new Interactor(socket, -1, false, bufferManager, token)),
                        error => { throw new ApplicationException("Failed to connect to authenticator", error); },
                        token);

            _market = market;
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
