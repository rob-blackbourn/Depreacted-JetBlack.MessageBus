using System;
using System.Net.Sockets;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using JetBlack.MessageBus.TopicBus.Messages;
using BufferManager = System.ServiceModel.Channels.BufferManager;

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
}
