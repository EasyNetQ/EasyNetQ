// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Required a RabbitMQ instance on localhost")]
    public class MultipleHandlerPerConsumerTests
    {
        private IBus bus;

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test]
        public void Should_cosume_multiple_message_types()
        {
            var countdownEvent = new CountdownEvent(3);

            var queue = bus.Advanced.QueueDeclare("multiple_types");

            bus.Advanced.Consume(queue, x => x
                    .Add<MyMessage>((message, info) => 
                        { 
                            Console.WriteLine("Got MyMessage {0}", message.Body.Text);
                            countdownEvent.Signal();
                        })
                    .Add<MyOtherMessage>((message, info) =>
                        {
                            Console.WriteLine("Got MyOtherMessage {0}", message.Body.Text);
                            countdownEvent.Signal();
                        })
                    .Add<IAnimal>((message, info) =>
                        {
                            Console.WriteLine("Got IAnimal of type {0}", message.Body.GetType().Name);
                            countdownEvent.Signal();
                        })
                );

            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, false, 
                new Message<MyMessage>(new MyMessage { Text = "Hello" }));
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, false, 
                new Message<MyOtherMessage>(new MyOtherMessage { Text = "Hi" }));
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, false, 
                new Message<Dog>(new Dog()));

            countdownEvent.Wait(1000);
        }
    }
}

// ReSharper restore InconsistentNaming