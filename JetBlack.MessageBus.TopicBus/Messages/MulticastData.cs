using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class MulticastData : Message
    {
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public MulticastData(string topic, bool isImage, byte[] data)
            : base(MessageType.MulticastData)
        {
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public MulticastData ReadBody(Stream stream)
        {
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = stream.ReadByteArray();
            return new MulticastData(topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Topic);
            stream.Write(IsImage);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Topic={1}, IsImage={2}, Data={3}", base.ToString(), Topic.ToFormattedString(), IsImage, Data.ToFormattedString());
        }
    }

}
