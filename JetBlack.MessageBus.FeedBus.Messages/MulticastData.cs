using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class MulticastData : Message
    {
        public readonly string Feed;
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public MulticastData(string feed, string topic, bool isImage, byte[] data)
            : base(MessageType.MulticastData)
        {
            Feed = feed;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public MulticastData ReadBody(Stream stream)
        {
            var feed = stream.ReadString();
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = stream.ReadByteArray();
            return new MulticastData(feed, topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Feed);
            stream.Write(Topic);
            stream.Write(IsImage);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Feed={1}, Topic={2}, IsImage={3}, Data={4}", base.ToString(), Feed.ToFormattedString(), Topic.ToFormattedString(), IsImage, Data.ToFormattedString());
        }
    }
}
