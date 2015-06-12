using System;
using System.Net;
using System.ServiceModel.Channels;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Distributor;

namespace JetBlack.MessageBus.Examples.TopicBusConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;
            var ipAddress = IPAddress.Any;
            const int port = 9090;

            var cts = new CancellationTokenSource();
            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            var endpoint = new IPEndPoint(ipAddress, port);

            var server = new Server(endpoint, null, bufferManager, cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            server.Dispose();
            cts.Cancel();
        }
    }
}
