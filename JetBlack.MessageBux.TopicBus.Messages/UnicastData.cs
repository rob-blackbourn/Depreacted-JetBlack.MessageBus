using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class UnicastData : Message
    {
        public readonly int ClientId;
        public readonly string Topic;
        public readonly bool IsImage;
        public readonly byte[] Data;

        public UnicastData(int clientId, string topic, bool isImage, byte[] data)
            : base(MessageType.UnicastData)
        {
            ClientId = clientId;
            Topic = topic;
            IsImage = isImage;
            Data = data;
        }

        static public UnicastData ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var topic = stream.ReadString();
            var isImage = stream.ReadBoolean();
            var data = stream.ReadByteArray();
            return new UnicastData(clientId, topic, isImage, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(Topic);
            stream.Write(IsImage);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, ClientId={1}, Topic={2}, IsImage={3}, Data={4}", base.ToString(), ClientId, Topic.ToFormattedString(), IsImage, Data.ToFormattedString());
        }
    }
}
