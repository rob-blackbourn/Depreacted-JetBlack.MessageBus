﻿using System.IO;
using JetBlack.MessageBus.Common.Diagnostics;
using JetBlack.MessageBus.Common.IO;

namespace JetBlack.MessageBus.TopicBus.Messages
{
    public class AuthenticationResponse : Message
    {
        public readonly int ClientId;
        public readonly AuthenticationStatus Status;
        public readonly byte[] Data;

        public AuthenticationResponse(int clientId, AuthenticationStatus status, byte[] data)
            : base(MessageType.AuthenticationResponse)
        {
            ClientId = clientId;
            Status = status;
            Data = data;
        }

        static public AuthenticationResponse ReadBody(Stream stream)
        {
            var clientId = stream.ReadInt32();
            var status = (AuthenticationStatus)stream.ReadByte();
            var data = stream.ReadByteArray();
            return new AuthenticationResponse(clientId, status, data);
        }

        public override Stream Write(Stream stream)
        {
            base.Write(stream);
            stream.Write(ClientId);
            stream.Write((byte)Status);
            stream.Write(Data);
            return stream;
        }

        public override string ToString()
        {
            return string.Format("{0}, ClientId={1}, Status={2}, Data={3}", base.ToString(), ClientId, Status, Data.ToFormattedString());
        }
    }
}