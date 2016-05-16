using System.Net;

namespace JetBlack.MessageBus.FeedBus.Distributor.Config
{
    public class ClientConfig
    {
        public ClientConfig(IPAddress ipAddress, string user, ClientRole allow, ClientRole deny)
        {
            IPAddress = ipAddress;
            User = user;
            Allow = allow;
            Deny = deny;
        }

        public IPAddress IPAddress { get; private set; }
        public string User { get; private set; }
        public ClientRole Allow { get; private set; }
        public ClientRole Deny { get; private set; }
    }
}
