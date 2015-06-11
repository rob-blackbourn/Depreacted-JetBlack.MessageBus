using System;
using System.Net;
using System.Threading;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        readonly Market _market;

        public Server(IPEndPoint endPoint, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            Log.Info("Starting server");

            _market = new Market(new Acceptor(maxBufferPoolSize, maxBufferSize).ToObservable(endPoint, token));
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
