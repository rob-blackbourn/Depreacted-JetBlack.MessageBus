using System;
using System.Configuration;
using System.Net;
using System.ServiceModel.Channels;
using JetBlack.MessageBus.FeedBus.Distributor;
using JetBlack.MessageBus.FeedBus.Distributor.Config;

namespace JetBlack.MessageBus.Examples.FeedBusConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var configuration = (ConfigurationSectionHandler)ConfigurationManager.GetSection("authFeedBusDistributor");

            const int maxBufferPoolSize = 100;
            const int maxBufferSize = 100000;
            var ipAddress = IPAddress.Any;
            const int port = 9090;

            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            var endpoint = new IPEndPoint(ipAddress, port);

            var server = new Server(endpoint, bufferManager, configuration.DefaultConfig);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            server.Dispose();
        }
    }
}
