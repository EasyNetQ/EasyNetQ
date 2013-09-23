// ReSharper disable InconsistentNaming

using NUnit.Framework;
using Rhino.Mocks;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    [TestFixture]
    public class When_disposed : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.Stub(x => x.IsConnected).Return(true);
            consumer.StartConsuming();
            consumer.Dispose();
        }

        [Test]
        public void Should_dispose_the_internal_consumer()
        {
            internalConsumers[0].AssertWasCalled(x => x.Dispose());
        }
    }
}