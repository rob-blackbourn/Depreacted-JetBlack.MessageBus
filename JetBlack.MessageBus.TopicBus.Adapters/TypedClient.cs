using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Threading;
using JetBlack.MessageBus.Common.IO;
using System.Net;
using System.Threading.Tasks;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class TypedClient<TData> : Client
    {
        public event EventHandler<DataReceivedEventArgs<TData>> OnDataReceived;

        private readonly IByteEncoder<TData> _byteEncoder;

        public TypedClient(TcpClient tcpClient, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
            : base(tcpClient, maxBufferPoolSize, maxBufferSize, scheduler, token)
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

        protected override void RaiseOnData(string topic, byte[] data, bool isImage)
        {
            var handler = OnDataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs<TData>(topic, _byteEncoder.Decode(data), isImage));
        }

        public static async Task<TypedClient<TData>> Create(IPEndPoint endpoint, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);

            return new TypedClient<TData>(tcpClient, byteEncoder, maxBufferPoolSize, maxBufferSize, scheduler, token);
        }
    }
}