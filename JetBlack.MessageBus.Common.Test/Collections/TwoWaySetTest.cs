using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using JetBlack.MessageBus.Common.Collections;

namespace JetBlack.MessageBus.Common.Test.Collections
{
    [TestClass]
    public class TwoWaySetTest
    {
        [TestMethod]
        public void SmokeTest()
        {
            var twoWaySet = new TwoWaySet<string, int>();

            const string a = "a", b = "b", c = "c";
            const int one = 1, two = 2, three = 3;

            twoWaySet.Add(a, one);
            twoWaySet.Add(two, b);
            twoWaySet.Add(b, three);
            Assert.IsTrue(twoWaySet.ContainsKey(a));
            Assert.IsTrue(twoWaySet.ContainsKey(one));
            Assert.IsTrue(twoWaySet.ContainsKey(b));
            Assert.IsTrue(twoWaySet.ContainsKey(two));
            Assert.IsTrue(twoWaySet.ContainsKey(three));
            Assert.IsFalse(twoWaySet.ContainsKey(c));

            ISet<string> words;
            Assert.IsTrue(twoWaySet.TryGetValue(one, out words));
            Assert.AreEqual(1, words.Count);
            ISet<int> numbers;
            Assert.IsTrue(twoWaySet.TryGetValue(b, out numbers));
            Assert.AreEqual(2, numbers.Count);
            Assert.IsFalse(twoWaySet.TryGetValue(c, out numbers));
        }
    }
}
