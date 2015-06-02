using System;
using System.Net;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Distributor;

namespace JetBlack.MessageBus.Examples.TopicBusConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();

            var cts = new CancellationTokenSource();
            var server = new Server(new IPEndPoint(IPAddress.Any, 9090), cts.Token);

            Console.WriteLine("Press <ENTER> to quit");
            Console.ReadLine();

            server.Dispose();
            cts.Cancel();
        }
    }
}
