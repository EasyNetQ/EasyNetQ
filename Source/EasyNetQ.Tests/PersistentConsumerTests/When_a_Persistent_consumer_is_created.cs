// ReSharper disable InconsistentNaming

using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    [TestFixture]
    public class When_a_Persistent_consumer_starts_consuming : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.Stub(x => x.IsConnected).Return(true);
            consumer.StartConsuming();
        }

        [Test]
        public void Should_create_internal_consumer()
        {
            internalConsumerFactory.AssertWasCalled(x => x.CreateConsumer());
            createConsumerCalled.ShouldEqual(1);
        }

        [Test]
        public void Should_ask_the_internal_consumer_to_start_consuming()
        {
            internalConsumers[0].AssertWasCalled(x => x.StartConsuming(persistentConnection, queue, onMessage, configuration));
        }
    }
}

// ReSharper restore InconsistentNaming