// ReSharper disable InconsistentNaming

using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConsumerErrorConditionsTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("localhost");

            // wait for rabbit to connect
            while (!bus.IsConnected)
            {
                Thread.Sleep(10);
            }
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_log_exceptions_thrown_by_subscribers()
        {
            
        }
    }
}

// ReSharper restore InconsistentNaming