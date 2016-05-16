using System;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal struct FeedAndTopic : IEquatable<FeedAndTopic>, IComparable<FeedAndTopic>
    {
        public FeedAndTopic(string feed, string topic)
        {
            Feed = feed;
            Topic = topic;
        }

        public string Feed { get; private set; }
        public string Topic { get; private set; }

        public int CompareTo(FeedAndTopic other)
        {
            var diff = string.Compare(Feed, other.Feed);
            if (diff != 0)
                return diff;
            return string.Compare(Topic, other.Topic);
        }

        public bool Equals(FeedAndTopic other)
        {
            return string.Equals(Feed, other.Feed) && string.Equals(Topic, other.Topic);
        }

        public override bool Equals(object obj)
        {
            return obj is FeedAndTopic && Equals((FeedAndTopic)obj);
        }

        public override int GetHashCode()
        {
            return (Feed == null ? 0 : Feed.GetHashCode()) ^ (Topic == null ? 0 : Topic.GetHashCode());
        }

        public override string ToString()
        {
            return string.Format("Feed=\"{0}\", Topic=\"{1}\"", Feed, Topic);
        }
    }
}
