using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class SubscriptionRequest : Message
    {
        public readonly string Topic;
        public readonly bool IsAdd;

        public SubscriptionRequest(string topic, bool isAdd)
            : base(MessageType.SubscriptionRequest)
        {
            Topic = topic;
            IsAdd = isAdd;
        }

        static public SubscriptionRequest ReadBody(Stream stream)
        {
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new SubscriptionRequest(topic, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Topic={1}, IsAdd={2}", base.ToString(), Topic.ToFormattedString(), IsAdd);
        }
    }
}
