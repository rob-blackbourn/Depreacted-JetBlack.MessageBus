namespace JetBlack.MessageBus.TopicBus.Messages
{
    public enum AuthenticationStatus : byte
    {
        None,
        Required,
        Requested,
        Accepted,
        Rejected,
    }
}
