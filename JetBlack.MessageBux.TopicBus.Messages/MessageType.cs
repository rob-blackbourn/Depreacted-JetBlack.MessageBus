namespace JetBlack.MessageBus.TopicBus.Messages
{
    public enum MessageType : byte
    {
        MulticastData,
        UnicastData,
        ForwardedSubscriptionRequest,
        NotificationRequest,
        SubscriptionRequest
    }
}
