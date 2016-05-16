using System.Collections.Generic;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class PublisherRepository
    {
        private readonly TwoWaySet<FeedAndTopic, IInteractor> _topicsAndPublishers = new TwoWaySet<FeedAndTopic, IInteractor>();

        public void AddPublisher(IInteractor publisher, string feed, string topic)
        {
            _topicsAndPublishers.Add(publisher, new FeedAndTopic(feed, topic));
        }

        public IEnumerable<FeedAndTopic> RemovePublisher(IInteractor publisher)
        {
            return _topicsAndPublishers.Remove(publisher);
        }
    }
}
