using System;
using System.Net;
using System.ServiceModel.Channels;
using log4net;
using JetBlack.MessageBus.FeedBus.Distributor.Config;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Market _market;

        public Server(IPEndPoint serverEndPoint, BufferManager bufferManager, DistributorConfig config)
        {
            Log.Info("Starting server");
            _market = new Market(new Acceptor(bufferManager, config).ToObservable(serverEndPoint));
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
