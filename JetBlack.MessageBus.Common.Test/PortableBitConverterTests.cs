using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JetBlack.MessageBus.Common.Test
{
    [TestClass]
    public class PortableBitConverterTests
    {
        [TestMethod]
        public void ShouldRoundTrip()
        {
            AssertRoundTrip((short)1234, PortableBitConverter.GetBytes, PortableBitConverter.ToInt16);
            AssertRoundTrip(1234, PortableBitConverter.GetBytes, PortableBitConverter.ToInt32);
            AssertRoundTrip(1234L, PortableBitConverter.GetBytes, PortableBitConverter.ToInt64);
            AssertRoundTrip(1234.56f, PortableBitConverter.GetBytes, PortableBitConverter.ToFloat);
            AssertRoundTrip(1234.56, PortableBitConverter.GetBytes, PortableBitConverter.ToDouble);
            AssertRoundTrip('x', PortableBitConverter.GetBytes, PortableBitConverter.ToChar);
            AssertRoundTrip(new DateTime(2013, 3, 25, 12, 15, 3, 123), PortableBitConverter.GetBytes, PortableBitConverter.ToDateTime);
        }

        private void AssertRoundTrip<T>(T value, Func<T, byte[]> convertFrom, Func<byte[], int, T> convertTo)
        {
            var bytes = convertFrom(value);
            var roundTrip = convertTo(bytes, 0);
            Assert.AreEqual(value, roundTrip);
        }
    }
}
