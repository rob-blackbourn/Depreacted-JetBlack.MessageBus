using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class SubscriptionRequest : Message
    {
        public readonly string Feed;
        public readonly string Topic;
        public readonly bool IsAdd;

        public SubscriptionRequest(string feed, string topic, bool isAdd)
            : base(MessageType.SubscriptionRequest)
        {
            Feed = feed;
            Topic = topic;
            IsAdd = isAdd;
        }

        static public SubscriptionRequest ReadBody(Stream stream)
        {
            var feed= stream.ReadString();
            var topic = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new SubscriptionRequest(feed, topic, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Feed);
            stream.Write(Topic);
            stream.Write(IsAdd);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Feed={1}, Topic={2}, IsAdd={3}", base.ToString(), Feed.ToFormattedString(), Topic.ToFormattedString(), IsAdd);
        }
    }
}
