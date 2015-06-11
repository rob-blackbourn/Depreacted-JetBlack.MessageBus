using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class ForwardedAuthenticationRequest : Message
    {
        public readonly int ClientId;
        public readonly byte[] Data;

        public ForwardedAuthenticationRequest(int clientId, byte[] data)
            : base(MessageType.ForwardedAuthenticationRequest)
        {
            ClientId = clientId;
            Data = data;
        }

        static public ForwardedAuthenticationRequest ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var data = stream.ReadByteArray();
            return new ForwardedAuthenticationRequest(clientId, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, ClientId={1}, Data={2}", base.ToString(), ClientId, Data.ToFormattedString());
        }
    }
}
