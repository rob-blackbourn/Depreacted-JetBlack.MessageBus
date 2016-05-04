using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class NotificationRequest : Message
    {
        public readonly string TopicPattern;
        public readonly bool IsAdd;

        public NotificationRequest(string topicPattern, bool isAdd)
            : base(MessageType.NotificationRequest)
        {
            TopicPattern = topicPattern;
            IsAdd = isAdd;
        }

        static public NotificationRequest ReadBody(Stream stream)
        {
            var topicPattern = stream.ReadString();
            var isAdd = stream.ReadBoolean();
            return new NotificationRequest(topicPattern, isAdd);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(TopicPattern);
            stream.Write(IsAdd);
            return stream;
        }

        override public string ToString()
        {
            return string.Format("{0}, TopicPattern={1}, IsAdd={2}", base.ToString(), TopicPattern.ToFormattedString(), IsAdd);
        }
    }
}
