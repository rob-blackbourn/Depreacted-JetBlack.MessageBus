using System;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class ForwardedSubscriptionEventArgs : EventArgs
    {
        public ForwardedSubscriptionEventArgs(int clientId, string topic, bool isAdd)
        {
            ClientId = clientId;
            Topic = topic;
            IsAdd = isAdd;
        }

        public int ClientId { get; private set; }
        public string Topic { get; private set; }
        public bool IsAdd { get; private set; }
    }
}
