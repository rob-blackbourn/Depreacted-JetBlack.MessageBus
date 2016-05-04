using System;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class DataReceivedEventArgs<TData> : EventArgs
    {
        public DataReceivedEventArgs(string topic, TData data, bool isImage)
        {
            IsImage = isImage;
            Data = data;
            Topic = topic;
        }

        public string Topic { get; private set; }
        public TData Data { get; private set; }
        public bool IsImage { get; private set; }
    }
}
