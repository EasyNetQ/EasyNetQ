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
    public class When_a_consumer_is_cancelled_by_the_broker : IDisposable
    {
        private MockBuilder mockBuilder;

        public When_a_consumer_is_cancelled_by_the_broker()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("my_queue", false);

            mockBuilder.Bus.Advanced.Consume(queue, (bytes, properties, arg3) => Task.Factory.StartNew(() => { }));

            var are = new AutoResetEvent(false);
            mockBuilder.EventBus.Subscribe<ConsumerModelDisposedEvent>(x => are.Set());

            mockBuilder.Consumers[0].HandleBasicCancel("consumer_tag");

            are.WaitOne(500);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        [Fact]
        public void Should_dispose_of_the_model()
        {
            mockBuilder.Consumers[0].Model.Received().Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming