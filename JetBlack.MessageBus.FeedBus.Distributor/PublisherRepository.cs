using System.Collections.Generic;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.FeedBus.Distributor
{
    internal class PublisherRepository
    {
        private readonly TwoWaySet<KeyValuePair<string,string>, IInteractor> _topicsAndPublishers = new TwoWaySet<KeyValuePair<string, string>, IInteractor>();

        public void AddPublisher(IInteractor publisher, string feed, string topic)
        {
            _topicsAndPublishers.Add(publisher, KeyValuePair.Create(feed, topic));
        }

        public IEnumerable<KeyValuePair<string,string>> RemovePublisher(IInteractor publisher)
        {
            return _topicsAndPublishers.Remove(publisher);
        }
    }
}
