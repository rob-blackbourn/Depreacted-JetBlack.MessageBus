using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class AuthenticationRequest : Message
    {
        public readonly byte[] Data;

        public AuthenticationRequest(byte[] data)
            : base(MessageType.AuthenticationRequest)
        {
            Data = data;
        }

        static public AuthenticationRequest ReadBody(Stream stream)
        {
            var data = stream.ReadByteArray();
            return new AuthenticationRequest(data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Data={1}", base.ToString(), Data.ToFormattedString());
        }
    }
}
