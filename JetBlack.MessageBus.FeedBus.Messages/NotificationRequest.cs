using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class NotificationRequest : Message
    {
        public readonly string Feed;
        public readonly string TopicPattern;
        public readonly bool IsAdd;

        public NotificationRequest(string feed, string topicPattern, bool isAdd)
            : base(MessageType.NotificationRequest)
        {
            Feed = feed;
            TopicPattern = topicPattern;
            IsAdd = isAdd;
        }

        static public NotificationRequest ReadBody(Stream stream)
        {
            var feed = stream.ReadString();
            var topicPattern = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new NotificationRequest(feed, topicPattern, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Feed);
            stream.Write(TopicPattern);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0}, Feed={1}, TopicPattern={2}, IsAdd={3}", base.ToString(), Feed.ToFormattedString(), TopicPattern.ToFormattedString(), IsAdd);
        }
    }
}
