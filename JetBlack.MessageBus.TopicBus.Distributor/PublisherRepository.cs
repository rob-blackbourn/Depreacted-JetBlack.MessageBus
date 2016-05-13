using System.Collections.Generic;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.TopicBus.Distributor
{
    internal class PublisherRepository
    {
        private readonly TwoWaySet<string, IInteractor> _topicsAndPublishers = new TwoWaySet<string, IInteractor>();

        public void AddPublisher(IInteractor publisher, string topic)
        {
            _topicsAndPublishers.Add(publisher, topic);
        }

        public IEnumerable<string> RemovePublisher(IInteractor publisher)
        {
            return _topicsAndPublishers.Remove(publisher);
        }
    }
}
