// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class CorrelationIdTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = new TestBusFactory().CreateBusWithMockAmqpClient();
        }

        [Test]
        public void Should_write_correlation_id_to_properties()
        {
            bus.Publish(new TestMessage());
        }
    }
}

// ReSharper restore InconsistentNaming