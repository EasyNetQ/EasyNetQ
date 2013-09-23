// ReSharper disable InconsistentNaming

using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    [TestFixture]
    public class When_disposed_and_internal_consumer_is_cancelled : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.Stub(x => x.IsConnected).Return(true);
            consumer.StartConsuming();
            internalConsumers[0].Raise(x => x.Cancelled += y => { }, internalConsumers[0]);
            consumer.Dispose();
        }

        [Test]
        public void Should_not_dispose_the_internal_consumer()
        {
            internalConsumers[0].AssertWasNotCalled(x => x.Dispose());
        }

        [Test]
        public void Should_dispose_the_recreated_consumer()
        {
            internalConsumers.Count.ShouldEqual(2);
            internalConsumers[1].AssertWasCalled(x => x.Dispose());
        }
    }
}