using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class NotificationRequest : Message
    {
        public readonly string Feed;
        public readonly bool IsAdd;

        public NotificationRequest(string feed, bool isAdd)
            : base(MessageType.NotificationRequest)
        {
            Feed = feed;
            IsAdd = isAdd;
        }

        static public NotificationRequest ReadBody(Stream stream)
        {
            var feed = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new NotificationRequest(feed, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Feed);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0}, Feed={1}, IsAdd={2}", base.ToString(), Feed.ToFormattedString(), IsAdd);
        }
    }
}
