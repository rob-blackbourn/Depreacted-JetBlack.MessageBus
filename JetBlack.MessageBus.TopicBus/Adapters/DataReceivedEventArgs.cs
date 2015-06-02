using System;

namespace JetBlack.MessageBus.TopicBus.Adapters
{
    public class DataReceivedEventArgs<T> : EventArgs
    {
        public DataReceivedEventArgs(string topic, T data, bool isImage)
        {
            IsImage = isImage;
            Data = data;
            Topic = topic;
        }

        public string Topic { get; private set; }
        public T Data { get; private set; }
        public bool IsImage { get; private set; }
    }
}
