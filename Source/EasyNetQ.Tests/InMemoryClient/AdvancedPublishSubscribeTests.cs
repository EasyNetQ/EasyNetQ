// ReSharper disable InconsistentNaming

using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.InMemoryClient;
using NUnit.Framework;

namespace EasyNetQ.Tests.InMemoryClient
{
    [TestFixture]
    public class AdvancedPublishSubscribeTests
    {
        private IAdvancedBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = InMemoryRabbitHutch.CreateBus().Advanced;
        }

        [Test, Explicit("Failing on build server for some reason")]
        public void Should_not_overwrite_correlation_id()
        {
            var autoResetEvent = new AutoResetEvent(false);
            const string expectedCorrelationId = "abc_foo";
            var actualCorrelationId = "";

            var queue = EasyNetQ.Topology.Queue.DeclareDurable("myqueue");
            var exchange = EasyNetQ.Topology.Exchange.DeclareDirect("myexchange");
            queue.BindTo(exchange, "#");
            bus.Subscribe<MyMessage>(queue, (message, info) => Task.Factory.StartNew(() =>
            {
                actualCorrelationId = message.Properties.CorrelationId;
                autoResetEvent.Set();
            }));

            var messageToSend = new Message<MyMessage>(new MyMessage());
            messageToSend.Properties.CorrelationId = expectedCorrelationId;

            using (var channel = bus.OpenPublishChannel())
            {
                channel.Publish(exchange, "abc", messageToSend);
            }

            autoResetEvent.WaitOne(1000);

            actualCorrelationId.ShouldEqual(expectedCorrelationId);
        }
    }
}

// ReSharper restore InconsistentNaming