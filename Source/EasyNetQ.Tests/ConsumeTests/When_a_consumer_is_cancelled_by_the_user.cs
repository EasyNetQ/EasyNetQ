﻿// ReSharper disable InconsistentNaming

using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using Xunit;
using NSubstitute;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_consumer_is_cancelled_by_the_user
    {
        private MockBuilder mockBuilder;

        public When_a_consumer_is_cancelled_by_the_user()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("my_queue", false);

            var cancelSubscription = mockBuilder.Bus.Advanced
                .Consume(queue, (bytes, properties, arg3) => Task.Factory.StartNew(() => { }));

            var are = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());

            cancelSubscription.Dispose();

            are.WaitOne(500);
        }

        [TearDown]
        public void TearDown()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_dispose_the_model()
        {
            mockBuilder.Consumers[0].Model.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming