using System;
using System.Net;
using System.Reactive.Subjects;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Messages;
using log4net;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    public class Server : IDisposable
    {
        private static readonly ILog Log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Market _market;

        public Server(IPEndPoint serverEndPoint, ISubject<ForwardedAuthenticationRequest,AuthenticationResponse> authenticator, int maxBufferPoolSize, int maxBufferSize, CancellationToken token)
        {
            Log.Info("Starting server");

            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            var acceptor = new Acceptor(serverEndPoint, bufferManager);
            _market = new Market(acceptor.ToObservable(false, token), authenticator);
        }

        public void Dispose()
        {
            _market.Dispose();
        }
    }
}
