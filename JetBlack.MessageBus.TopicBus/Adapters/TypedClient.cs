using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class TypedClient<TData> : Client
    {
        public event EventHandler<DataReceivedEventArgs<TData>> OnDataReceived;
        public event EventHandler<AuthenticationResponseEventArgs<TData>> OnAuthenticationResponse;

        private readonly IByteEncoder<TData> _byteEncoder;

        public TypedClient(Socket socket, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(socket, maxBufferPoolSize, maxBufferSize, scheduler, token)
        {
            _byteEncoder = byteEncoder;
        }

        public void Send(int clientId, string topic, bool isImage, TData data)
        {
            Send(clientId, topic, isImage, _byteEncoder.Encode(data));
        }

        public void Publish(string topic, bool isImage, TData data)
        {
            Publish(topic, isImage, _byteEncoder.Encode(data));
        }

        public void RequestAuthentication(TData data)
        {
            RequestAuthentication(_byteEncoder.Encode(data));
        }

        protected override void RaiseOnData(string topic, byte[] data, bool isImage)
        {
            var handler = OnDataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs<TData>(topic, _byteEncoder.Decode(data), isImage));
        }

        protected override void RaiseOnAuthenticationResponse(AuthenticationStatus status, byte[] data)
        {
            var handler = OnAuthenticationResponse;
            if (handler != null)
                OnAuthenticationResponse(this, new AuthenticationResponseEventArgs<TData>(status, _byteEncoder.Decode(data)));
        }
    }
}