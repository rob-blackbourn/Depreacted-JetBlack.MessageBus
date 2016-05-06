using System;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BufferManager = System.ServiceModel.Channels.BufferManager;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.TopicBus.Messages;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public abstract class Client
    {
        public event EventHandler<ForwardedSubscriptionEventArgs> OnForwardedSubscription;

        private readonly IObserver<Message> _messageObserver;

        protected Client(TcpClient tcpClient, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
        {
            // TODO: Howdo we close the client if we don't store the client?
            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            tcpClient.ToMessageObservable(bufferManager).SubscribeOn(scheduler).Subscribe(Dispatch, token);
            _messageObserver = tcpClient.ToMessageObserver(bufferManager);
        }

        private void Dispatch(Message message)
        {
            switch (message.MessageType)
            {
                case MessageType.MulticastData:
                    RaiseOnData(((MulticastData)message).Topic, ((MulticastData)message).Data, false);
                    break;
                case MessageType.UnicastData:
                    RaiseOnData(((UnicastData)message).Topic, ((UnicastData)message).Data, true);
                    break;
                case MessageType.ForwardedSubscriptionRequest:
                    RaiseOnForwardedSubscriptionRequest((ForwardedSubscriptionRequest)message);
                    break;
                default:
                    throw new ArgumentException("invalid message type");
            }
        }

        public void AddSubscription(string topic)
        {
            _messageObserver.OnNext(new SubscriptionRequest(topic, true));
        }

        public void RemoveSubscription(string topic)
        {
            _messageObserver.OnNext(new SubscriptionRequest(topic, false));
        }

        protected void Send(int clientId, string topic, bool isImage, byte[] data)
        {
            _messageObserver.OnNext(new UnicastData(clientId, topic, isImage, data));
        }

        protected void Publish(string topic, bool isImage, byte[] data)
        {
            _messageObserver.OnNext(new MulticastData(topic, isImage, data));
        }

        public void AddNotification(string topicPattern)
        {
            _messageObserver.OnNext(new NotificationRequest(topicPattern, true));
        }

        public void RemoveNotification(string topicPattern)
        {
            _messageObserver.OnNext(new NotificationRequest(topicPattern, false));
        }

        private void RaiseOnForwardedSubscriptionRequest(ForwardedSubscriptionRequest message)
        {
            var handler = OnForwardedSubscription;
            if (handler != null)
                handler(this, new ForwardedSubscriptionEventArgs(message.ClientId, message.Topic, message.IsAdd));
        }

        protected abstract void RaiseOnData(string topic, byte[] data, bool isImage);
    }

    public class Client<TData> : Client
    {
        public event EventHandler<DataReceivedEventArgs<TData>> OnDataReceived;

        private readonly IByteEncoder<TData> _byteEncoder;

        public Client(TcpClient tcpClient, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
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

        public static async Task<Client<TData>> Create(IPEndPoint endpoint, IByteEncoder<TData> byteEncoder, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(endpoint.Address, endpoint.Port);

            return new Client<TData>(tcpClient, byteEncoder, maxBufferPoolSize, maxBufferSize, scheduler, token);
        }
    }
}
