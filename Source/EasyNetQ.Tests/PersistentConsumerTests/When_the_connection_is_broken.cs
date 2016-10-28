// ReSharper disable InconsistentNaming

using EasyNetQ.Events;
using NUnit.Framework;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    [TestFixture]
    public class When_the_connection_is_broken : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.IsConnected.Returns(true);
            consumer.StartConsuming();
            eventBus.Publish(new ConnectionCreatedEvent());
        }

        [Test]
        public void Should_re_create_internal_consumer()
        {
            internalConsumerFactory.Received().CreateConsumer();
            createConsumerCalled.ShouldEqual(2);
            internalConsumers.Count.ShouldEqual(2);
        }
    }
}