﻿// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NSubstitute;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_consumer_is_cancelled_by_the_user : IDisposable
    {
        private MockBuilder mockBuilder;

        public When_a_consumer_is_cancelled_by_the_user()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("my_queue", false);

            var cancelSubscription = mockBuilder.Bus.Advanced
                .Consume(queue, (bytes, properties, arg3) => Task.Run(() => { }));

            var are = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());

            cancelSubscription.Dispose();

            if (!are.WaitOne(5000))
            {
                throw new TimeoutException();
            }
        }

        public void Dispose()
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