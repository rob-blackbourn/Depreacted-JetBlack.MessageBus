using System.IO;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public abstract class Message
    {
        public readonly MessageType MessageType;

        protected Message(MessageType messageType)
        {
            MessageType = messageType;
        }

        public static Message Read(Stream stream)
        {
            var messageType = ReadHeader(stream);

            switch (messageType)
            {
                case MessageType.MulticastData:
                    return MulticastData.ReadBody(stream);
                case MessageType.UnicastData:
                    return UnicastData.ReadBody(stream);
                case MessageType.ForwardedSubscriptionRequest:
                    return ForwardedSubscriptionRequest.ReadBody(stream);
                case MessageType.NotificationRequest:
                    return NotificationRequest.ReadBody(stream);
                case MessageType.SubscriptionRequest:
                    return SubscriptionRequest.ReadBody(stream);
                default:
                    throw new InvalidDataException("unknown message type");
            }
        }

        private static MessageType ReadHeader(Stream stream)
        {
            var messageType = (MessageType)stream.ReadByte();
            return messageType;
        }

        public virtual Stream Write(Stream stream)
        {
            stream.Write((byte)MessageType);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("MessageType={0}", MessageType);
        }
    }
}
