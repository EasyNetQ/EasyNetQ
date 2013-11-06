// ReSharper disable InconsistentNaming

using EasyNetQ.Topology;
using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    class When_consume_is_cancelled_for_transient_consumer : ConsumerTestBase
    {
        protected override void AdditionalSetUp()
        {
            StartConsumer((body, properties, info) => { }, new Queue("my_queue", true));
        }

        [Test]
        public void Should_dispose_channel()
        {
            MockBuilder.Channels[0].BasicCancel(ConsumerTag);

            MockBuilder.Channels[0].AssertWasCalled(x => x.Dispose());
        }
    }
}

// ReSharper restore InconsistentNaming