// ReSharper disable InconsistentNaming

using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_disposed : Given_a_PersistentConsumer
    {
        public override void AdditionalSetup()
        {
            persistentConnection.IsConnected.Returns(true);
            consumer.StartConsuming();
            consumer.Dispose();
        }

        [Fact]
        public void Should_dispose_the_internal_consumer()
        {
            internalConsumers[0].Received().Dispose();
        }
    }
}