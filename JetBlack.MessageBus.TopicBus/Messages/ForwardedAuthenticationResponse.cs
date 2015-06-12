using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class ForwardedAuthenticationResponse : Message
    {
        public readonly AuthenticationStatus Status;
        public readonly byte[] Data;

        public ForwardedAuthenticationResponse(AuthenticationStatus status, byte[] data)
            : base(MessageType.ForwardedAuthenticationResponse)
        {
            Status = status;
            Data = data;
        }

        static public ForwardedAuthenticationResponse ReadBody(Stream stream)
        {
            var status = (AuthenticationStatus)stream.ReadByte();
            var data = stream.ReadByteArray();
            return new ForwardedAuthenticationResponse(status, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write((byte)Status);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, Status={1}, Data={2}", base.ToString(), Status, Data.ToFormattedString());
        }
    }
}
