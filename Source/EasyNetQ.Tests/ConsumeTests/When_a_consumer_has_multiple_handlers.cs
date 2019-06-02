// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using FluentAssertions;
using RabbitMQ.Client.Framing;
using Xunit;

namespace EasyNetQ.Tests.ConsumeTests
{
    public class When_a_consumer_has_multiple_handlers : IDisposable
    {
        public When_a_consumer_has_multiple_handlers()
        {
            mockBuilder = new MockBuilder();

            var queue = new Queue("test_queue", false);

            var countdownEvent = new CountdownEvent(3);

            mockBuilder.Bus.Advanced.Consume(
                queue,
                x => x.Add<MyMessage>((message, info) =>
                    {
                        myMessageResult = message.Body;
                        countdownEvent.Signal();
                    })
                    .Add<MyOtherMessage>((message, info) =>
                    {
                        myOtherMessageResult = message.Body;
                        countdownEvent.Signal();
                    })
                    .Add<IAnimal>((message, info) =>
                    {
                        animalResult = message.Body;
                        countdownEvent.Signal();
                    })
            );

            Deliver(new MyMessage { Text = "Hello Polymorphs!" });
            Deliver(new MyOtherMessage { Text = "Hello Isomorphs!" });
            Deliver(new Dog());

            if (!countdownEvent.Wait(5000)) throw new TimeoutException();
        }

        public void Dispose()
        {
            mockBuilder.Bus.Dispose();
        }

        private readonly MockBuilder mockBuilder;

        private MyMessage myMessageResult;
        private MyOtherMessage myOtherMessageResult;
        private IAnimal animalResult;

        private void Deliver<T>(T message) where T : class
        {
            var body = new JsonSerializer().MessageToBytes(typeof(T), message);
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
        public void Should_deliver_a_ploymorphic_message()
        {
            animalResult.Should().NotBeNull();
            animalResult.Should().BeOfType<Dog>();
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
    }
}

// ReSharper restore InconsistentNaming
