using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;
using System.Net;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class ForwardedSubscriptionRequest : Message
    {
        public readonly int ClientId;
        public readonly IPAddress IPAddress;
        public readonly string UserName;
        public readonly string Feed;
        public readonly string Topic;
        public readonly bool IsAdd;

        public ForwardedSubscriptionRequest(int clientId, IPAddress ipAddress, string userName, string feed, string topic, bool isAdd)
            : base(MessageType.ForwardedSubscriptionRequest)
        {
            ClientId = clientId;
            IPAddress = ipAddress;
            UserName = userName;
            Feed = feed;
            Topic = topic;
            IsAdd = isAdd;
        }

        static public ForwardedSubscriptionRequest ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var ipAddress = stream.ReadIPAddress();
            var userName = stream.ReadString();
            var feed = stream.ReadString();
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new ForwardedSubscriptionRequest(clientId, ipAddress, userName, feed, topic, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(IPAddress);
            stream.Write(UserName);
            stream.Write(Feed);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0}, ClientId={1}, IPAddress={2}, UserName={3}, Feed={4}, Topic={5}, IsAdd{6}", base.ToString(), ClientId, IPAddress, UserName, Feed.ToFormattedString(), Topic.ToFormattedString(), IsAdd);
        }
    }
}
