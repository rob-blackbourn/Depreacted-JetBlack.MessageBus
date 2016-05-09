using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.FeedBus.Messages
{
    public class UnicastData : Message
    {
        public readonly int ClientId;
        public readonly string Feed;
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public UnicastData(int clientId, string feed, string topic, bool isImage, byte[] data)
            : base(MessageType.UnicastData)
        {
            ClientId = clientId;
            Feed = feed;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public UnicastData ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var feed = stream.ReadString();
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = stream.ReadByteArray();
            return new UnicastData(clientId, feed, topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(Feed);
            stream.Write(Topic);
            stream.Write(IsImage);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, ClientId={1}, Feed={2}, Topic={3}, IsImage={4}, Data={5}", base.ToString(), ClientId, Feed.ToFormattedString(), Topic.ToFormattedString(), IsImage, Data.ToFormattedString());
        }
    }
}
