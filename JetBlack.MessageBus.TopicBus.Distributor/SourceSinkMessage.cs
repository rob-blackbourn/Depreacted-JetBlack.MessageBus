namespace JetBlack.MessageBus.TopicBus.Distributor
{
    static class SourceSinkMessage
    {
        public static SourceSinkMessage<T> Create<T>(IInteractor source, IInteractor sink, T content)
        {
            return new SourceSinkMessage<T>(source, sink, content);
        }
    }

    class SourceSinkMessage<T>
    {
        public IInteractor Source { get; private set; }
        public IInteractor Sink { get; private set; }
        public T Content { get; private set; }

        public SourceSinkMessage(IInteractor source, IInteractor sink, T content)
        {
            Source = source;
            Sink = sink;
            Content = content;
        }
    }
}
