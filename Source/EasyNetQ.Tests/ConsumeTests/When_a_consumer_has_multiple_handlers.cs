﻿// ReSharper disable InconsistentNaming
using System;
using System.Threading;
using EasyNetQ.Internals;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using FluentAssertions;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_consumer_has_multiple_handlers : IDisposable
    {
        private MockBuilder mockBuilder;

        private MyMessage myMessageResult;
        private MyOtherMessage myOtherMessageResult;
        private IAnimal animalResult;

        public When_a_consumer_has_multiple_handlers()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("test_queue", false);

            var countdownEvent = new CountdownEvent(3);

            mockBuilder.Bus.Advanced.Consume(queue, x => x
                .Add<MyMessage>((message, info) => TaskHelpers.ExecuteSynchronously(() =>
                    {
                        myMessageResult = message.Body;
                        countdownEvent.Signal();
                    }))
                .Add<MyOtherMessage>((message, info) => TaskHelpers.ExecuteSynchronously(() =>
                    {
                        myOtherMessageResult = message.Body;
                        countdownEvent.Signal();
                    }))
                .Add<IAnimal>((message, info) => TaskHelpers.ExecuteSynchronously(() =>
                    {
                        animalResult = message.Body;
                        countdownEvent.Signal();
                    })));

            Deliver(new MyMessage { Text = "Hello Polymorphs!" });
            Deliver(new MyOtherMessage { Text = "Hello Isomorphs!" });
            Deliver(new Dog());

            countdownEvent.Wait(5000);
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        void Deliver<T>(T message) where T : class
        {
            var body = new JsonSerializer().MessageToBytes(message);
            var properties = new BasicProperties
            {
                Type = new DefaultTypeNameSerializer().Serialize(typeof(T))
            };

            mockBuilder.Consumers[0].HandleBasicDeliver(
                "consumer_tag",
                0,
                false,
                "exchange",
                "routing_key",
                properties,
                body
                );
        }

        [Fact]
        public void Should_deliver_myMessage()
        {
            myMessageResult.Should().NotBeNull();
            myMessageResult.Text.Should().Be("Hello Polymorphs!");
        }

        [Fact]
        public void Should_deliver_myOtherMessage()
        {
            myOtherMessageResult.Should().NotBeNull();
            myOtherMessageResult.Text.Should().Be("Hello Isomorphs!");
        }

        [Fact]
        public void Should_deliver_a_ploymorphic_message()
        {
            animalResult.Should().NotBeNull();
            animalResult.Should().BeOfType<Dog>();
        }
    }
}

// ReSharper restore InconsistentNaming