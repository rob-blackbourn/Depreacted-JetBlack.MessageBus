using System.Collections.Generic;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherRepository
    {
        private readonly TwoWaySet<string, Interactor> _topicsAndPublishers = new TwoWaySet<string, Interactor>();

        public void AddPublisher(Interactor publisher, string topic)
        {
            _topicsAndPublishers.Add(publisher, topic);
        }

        public IEnumerable<string> RemovePublisher(Interactor publisher)
        {
            return _topicsAndPublishers.Remove(publisher);
        }
    }
}
