using Microsoft.VisualStudio.TestTools.UnitTesting;
using JetBlack.MessageBus.TopicBus.Distributor.Test.Mocks;

namespace JetBlack.MessageBus.TopicBus.Distributor.Test
{
    [TestClass]
    public class SubscriptionRepositoryTests
    {
        [TestMethod]
        public void ShouldRemoveAllSubscribers()
        {
            var repository = new SubscriptionRepository();

            var interactor = new MockInteractor(1, null, null);

            const string topic = "foo";
            repository.AddSubscription(interactor, topic);
            repository.AddSubscription(interactor, topic);
            Assert.AreEqual(repository.GetSubscribersToTopic(topic).Count, 1);
            repository.RemoveSubscription(interactor, topic, true);
            Assert.AreEqual(repository.GetSubscribersToTopic(topic), null);
        }
    }
}
