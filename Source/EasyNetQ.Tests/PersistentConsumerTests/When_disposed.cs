﻿// ReSharper disable InconsistentNaming

using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.PersistentConsumerTests
{
    public class When_disposed : Given_a_PersistentConsumer
    {
        public When_disposed()
        {
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
