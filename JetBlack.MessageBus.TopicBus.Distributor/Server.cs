using System;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Market _market;

        public Server(IPEndPoint serverEndPoint, BufferManager bufferManager)
        {
            Log.Info("Starting server");
            _market = new Market(new Acceptor(bufferManager).ToObservable(serverEndPoint));
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
