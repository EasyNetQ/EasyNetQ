// ReSharper disable InconsistentNaming
using EasyNetQ.Internals;
using RabbitMQ.Client.Framing;
using System.Threading;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.ConsumeTests
{
    [TestFixture]
    public class When_a_consumer_has_multiple_handlers
    {
        private MockBuilder mockBuilder;

        private MyMessage myMessageResult;
        private MyOtherMessage myOtherMessageResult;
        private IAnimal animalResult;

        [SetUp]
        public void SetUp()
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

            countdownEvent.Wait(1000);
        }

        public void Deliver<T>(T message) where T : class
        {
            var body = new JsonSerializer(new TypeNameSerializer()).MessageToBytes(message);
            var properties = new BasicProperties
            {
                Type = new TypeNameSerializer().Serialize(typeof(T))
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

        [Test]
        public void Should_deliver_myMessage()
        {
            myMessageResult.ShouldNotBeNull();
            myMessageResult.Text.ShouldEqual("Hello Polymorphs!");
        }

        [Test]
        public void Should_deliver_myOtherMessage()
        {
            myOtherMessageResult.ShouldNotBeNull();
            myOtherMessageResult.Text.ShouldEqual("Hello Isomorphs!");
        }

        [Test]
        public void Should_deliver_a_ploymorphic_message()
        {
            animalResult.ShouldNotBeNull();
            animalResult.ShouldBeOfType<Dog>();
        }
    }
}

// ReSharper restore InconsistentNaming