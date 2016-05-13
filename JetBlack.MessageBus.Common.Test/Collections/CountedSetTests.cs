using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetBlack.MessageBus.Common.Collections
{
    [TestClass]
    public class CountedSetTests
    {
        [TestMethod]
        public void ShouldCount()
        {
            var countedSet = new CountedSet<string>();

            const string a = "a", b = "b", c = "c";
            countedSet.Increment(a);
            countedSet.Increment(b);
            countedSet.Increment(c);
            Assert.AreEqual(3, countedSet.Count); // a:1, b:1, c:1
            countedSet.Increment(a);
            countedSet.Increment(b);
            Assert.AreEqual(3, countedSet.Count); // a:2, b:2, c:1
            countedSet.Decrement(b);
            countedSet.Decrement(c);
            Assert.AreEqual(2, countedSet.Count); // a:2, b:1, c:0
            countedSet.Decrement(b);
            Assert.AreEqual(1, countedSet.Count); // a:2, b:0, c:0
            countedSet.Delete(a);
            Assert.AreEqual(0, countedSet.Count); // a:0, b:0, c:0
        }

        [TestMethod]
        public void ShouldClear()
        {
            var countedSet = new CountedSet<string>();

            const string a = "a", b = "b", c = "c";
            countedSet.Increment(a);
            countedSet.Increment(b);
            countedSet.Increment(c);
            Assert.AreEqual(3, countedSet.Count);
            countedSet.Clear();
            Assert.AreEqual(0, countedSet.Count);
        }

        [TestMethod]
        public void ShouldContain()
        {
            var countedSet = new CountedSet<string>();

            const string a = "a", b = "b", c = "c";
            countedSet.Increment(a);
            countedSet.Increment(b);
            Assert.IsTrue(countedSet.Contains(a));
            Assert.IsTrue(countedSet.Contains(b));
            Assert.IsFalse(countedSet.Contains(c));
        }

        [TestMethod]
        public void ShouldTryGetCount()
        {
            var countedSet = new CountedSet<string>();

            int count;
            const string a = "a";
            countedSet.Increment(a);
            Assert.IsTrue(countedSet.TryGetCount(a, out count));
            Assert.AreEqual(1, count);
            countedSet.Increment(a);
            Assert.IsTrue(countedSet.TryGetCount(a, out count));
            Assert.AreEqual(2, count);
            countedSet.Increment(a);
            Assert.IsTrue(countedSet.TryGetCount(a, out count));
            Assert.AreEqual(3, count);
            countedSet.Decrement(a);
            Assert.IsTrue(countedSet.TryGetCount(a, out count));
            Assert.AreEqual(2, count);
            countedSet.Delete(a);
            Assert.IsFalse(countedSet.TryGetCount(a, out count));
        }
    }
}
