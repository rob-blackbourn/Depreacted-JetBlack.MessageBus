using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using JetBlack.MessageBus.Common.IO;
using JetBlack.MessageBus.TopicBus.Messages;
using BufferManager = System.ServiceModel.Channels.BufferManager;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class Client<T>
    {
        public event EventHandler<DataReceivedEventArgs<T>> OnDataReceived;
        public event EventHandler<ForwardedSubscriptionEventArgs> OnForwardedSubscription;

        private readonly IByteEncoder<T> _byteEncoder;
        private readonly IObserver<Message> _messageObserver;

        public Client(Socket socket, IByteEncoder<T> byteEncoder, IScheduler scheduler, CancellationToken token)
        {
            _byteEncoder = byteEncoder;
            var bufferManager = BufferManager.CreateBufferManager(100, 100000);
            socket.ToMessageObservable(bufferManager).SubscribeOn(scheduler).Subscribe(Dispatch, token);
            _messageObserver = socket.ToMessageObserver(bufferManager, token);
        }

        private void Dispatch(Message message)
        {
            switch (message.MessageType)
            {
                case MessageType.MulticastData:
                    RaiseOnData((MulticastData) message);
                    break;
                case MessageType.UnicastData:
                    RaiseOnData((UnicastData) message);
                    break;
                case MessageType.ForwardedSubscriptionRequest:
                    RaiseOnForwardedSubscriptionRequest((ForwardedSubscriptionRequest) message);
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

        public void Send(int clientId, string topic, bool isImage, T data)
        {
            _messageObserver.OnNext(new UnicastData(clientId, topic, isImage, _byteEncoder.Encode(data)));
        }

        public void Publish(string topic, bool isImage, T data)
        {
            _messageObserver.OnNext(new MulticastData(topic, isImage, _byteEncoder.Encode(data)));
        }

        public virtual void AddNotification(string topicPattern)
        {
            _messageObserver.OnNext(new NotificationRequest(topicPattern, true));
        }

        public virtual void RemoveNotification(string topicPattern)
        {
            _messageObserver.OnNext(new NotificationRequest(topicPattern, false));
        }

        private void RaiseOnForwardedSubscriptionRequest(ForwardedSubscriptionRequest message)
        {
            var handler = OnForwardedSubscription;
            if (handler != null)
                handler(this, new ForwardedSubscriptionEventArgs(message.ClientId, message.Topic, message.IsAdd));
        }

        private void RaiseOnData(MulticastData message)
        {
            RaiseOnData(message.Topic, _byteEncoder.Decode(message.Data), false);
        }

        private void RaiseOnData(UnicastData message)
        {
            RaiseOnData(message.Topic, _byteEncoder.Decode(message.Data), true);
        }

        private void RaiseOnData(string topic, T data, bool isImage)
        {
            var handler = OnDataReceived;
            if (handler != null)
                handler(this, new DataReceivedEventArgs<T>(topic, data, isImage));
        }
    }
}
