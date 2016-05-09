namespace JetBlack.MessageBus.FeedBus.Messages
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
