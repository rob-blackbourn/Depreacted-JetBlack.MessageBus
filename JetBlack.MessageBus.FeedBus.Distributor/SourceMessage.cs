namespace JetBlack.MessageBus.FeedBus.Distributor
{
    static class SourceMessage
    {
        public static SourceMessage<T> Create<T>(IInteractor source, T content)
        {
            return new SourceMessage<T>(source, content);
        }
    }

    class SourceMessage<T>
    {
        public IInteractor Source { get; private set; }
        public T Content { get; private set; }

        public SourceMessage(IInteractor source, T content)
        {
            Source = source;
            Content = content;
        }
    }
}
