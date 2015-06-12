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

        protected Client(Socket socket, int maxBufferPoolSize, int maxBufferSize, IScheduler scheduler, CancellationToken token)
        {
            var bufferManager = BufferManager.CreateBufferManager(maxBufferPoolSize, maxBufferSize);
            socket.ToMessageObservable(bufferManager).SubscribeOn(scheduler).Subscribe(Dispatch, token);
            _messageObserver = socket.ToMessageObserver(bufferManager, token);
        }

        private void Dispatch(Message message)
        {
            switch (message.MessageType)
            {
                case MessageType.MulticastData:
                    RaiseOnData((MulticastData)message);
                    break;
                case MessageType.UnicastData:
                    RaiseOnData((UnicastData)message);
                    break;
                case MessageType.ForwardedSubscriptionRequest:
                    RaiseOnForwardedSubscriptionRequest((ForwardedSubscriptionRequest)message);
                    break;
                case MessageType.ForwardedAuthenticationResponse:
                    RaiseOnAuthenticationResponse((ForwardedAuthenticationResponse)message);
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

        public void RequestAuthentication(byte[] data)
        {
            _messageObserver.OnNext(new AuthenticationRequest(data));
        }

        private void RaiseOnForwardedSubscriptionRequest(ForwardedSubscriptionRequest message)
        {
            var handler = OnForwardedSubscription;
            if (handler != null)
                handler(this, new ForwardedSubscriptionEventArgs(message.ClientId, message.Topic, message.IsAdd));
        }

        private void RaiseOnData(MulticastData message)
        {
            RaiseOnData(message.Topic, message.Data, false);
        }

        private void RaiseOnData(UnicastData message)
        {
            RaiseOnData(message.Topic, message.Data, true);
        }

        protected abstract void RaiseOnData(string topic, byte[] data, bool isImage);

        private void RaiseOnAuthenticationResponse(ForwardedAuthenticationResponse forwardedAuthenticationResponse)
        {
            RaiseOnAuthenticationResponse(forwardedAuthenticationResponse.Status, forwardedAuthenticationResponse.Data);
        }

        protected abstract void RaiseOnAuthenticationResponse(AuthenticationStatus status, byte[] data);
    }
}
