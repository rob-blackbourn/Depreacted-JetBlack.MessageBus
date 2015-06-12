using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class TypedClient<T> : Client
    {
        public event EventHandler<DataReceivedEventArgs<T>> OnDataReceived;
        public event EventHandler<AuthenticationResponseEventArgs<T>> OnAuthenticationResponse;

        private readonly IByteEncoder<T> _byteEncoder;

        public TypedClient(Socket socket, IByteEncoder<T> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(socket, maxBufferPoolSize, maxBufferSize, scheduler, token)
        {
            _byteEncoder = byteEncoder;
        }

        public void Send(int clientId, string topic, bool isImage, T data)
        {
            Send(clientId, topic, isImage, _byteEncoder.Encode(data));
        }

        public void Publish(string topic, bool isImage, T data)
        {
            Publish(topic, isImage, _byteEncoder.Encode(data));
        }

        public void RequestAuthentication(T data)
        {
            RequestAuthentication(_byteEncoder.Encode(data));
        }

        protected override void RaiseOnData(string topic, byte[] data, bool isImage)
        {
            var handler = OnDataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs<T>(topic, _byteEncoder.Decode(data), isImage));
        }

        protected override void RaiseOnAuthenticationResponse(AuthenticationStatus status, byte[] data)
        {
            var handler = OnAuthenticationResponse;
            if (handler != null)
                OnAuthenticationResponse(this, new AuthenticationResponseEventArgs<T>(status, _byteEncoder.Decode(data)));
        }
    }
}