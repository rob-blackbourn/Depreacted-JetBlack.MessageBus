using System;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.Common.Network;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Market _market;

        public Server(IPEndPoint serverEndPoint, IPEndPoint authenticatorEndpoint, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            Log.Info("Starting server");

            if (authenticatorEndpoint == null)
                Initialise(serverEndPoint, null, maxBufferPoolSize, maxBufferSize, token);
            else
                authenticatorEndpoint.ToConnectObservable()
                    .Subscribe(
                        socket => Initialise(serverEndPoint, socket, maxBufferPoolSize, maxBufferSize, token),
                        error => { throw new ApplicationException("Failed to connect to authenticator", error); },
                        token);
        }

        private void Initialise(IPEndPoint serverEndPoint, Socket authenticatorSocket, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            _market = new Market(serverEndPoint, authenticatorSocket, BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize), token);
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
