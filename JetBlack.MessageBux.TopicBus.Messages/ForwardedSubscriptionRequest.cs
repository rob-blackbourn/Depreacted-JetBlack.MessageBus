using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class ForwardedSubscriptionRequest : Message
    {
        public readonly int ClientId;
        public readonly string Topic;
        public readonly bool IsAdd;

        public ForwardedSubscriptionRequest(int clientId, string topic, bool isAdd)
            : base(MessageType.ForwardedSubscriptionRequest)
        {
            ClientId = clientId;
            Topic = topic;
            IsAdd = isAdd;
        }

        static public ForwardedSubscriptionRequest ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new ForwardedSubscriptionRequest(clientId, topic, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0}, ClientId={1}, Topic={2}, IsAdd{3}", base.ToString(), ClientId, Topic.ToFormattedString(), IsAdd);
        }
    }
}
