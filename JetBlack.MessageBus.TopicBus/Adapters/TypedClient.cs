using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class TypedClient<TData> : TypedClient<TData, TData>
    {
        public TypedClient(Socket socket, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(socket, byteEncoder, byteEncoder, maxBufferPoolSize, maxBufferSize, scheduler, token)
        {
        }
    }

    public class TypedClient<TData, TAuthenticationData> : Client
    {
        public event EventHandler<DataReceivedEventArgs<TData>> OnDataReceived;
        public event EventHandler<AuthenticationResponseEventArgs<TAuthenticationData>> OnAuthenticationResponse;

        private readonly IByteEncoder<TData> _byteEncoder;
        private readonly IByteEncoder<TAuthenticationData> _authenticationByteEncoder;

        public TypedClient(Socket socket, IByteEncoder<TData> byteEncoder, IByteEncoder<TAuthenticationData> authenticationByteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(socket, maxBufferPoolSize, maxBufferSize, scheduler, token)
        {
            _byteEncoder = byteEncoder;
            _authenticationByteEncoder = authenticationByteEncoder;
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
                OnAuthenticationResponse(this, new AuthenticationResponseEventArgs<TAuthenticationData>(status, _authenticationByteEncoder.Decode(data)));
        }
    }
}