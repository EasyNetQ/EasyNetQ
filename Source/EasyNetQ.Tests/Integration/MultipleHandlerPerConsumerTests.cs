// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Required a RabbitMQ instance on localhost")]
    public class MultipleHandlerPerConsumerTests : IDisposable
    {
        private IBus bus;

        public MultipleHandlerPerConsumerTests()
        {
            bus = RabbitHutch.CreateBus("host=localhost");
        }

        public void Dispose()
        {
            bus.Dispose();
        }

        [Fact]
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

            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, new Message<MyMessage>(new MyMessage { Text = "Hello" }));
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, new Message<MyOtherMessage>(new MyOtherMessage { Text = "Hi" }));
            bus.Advanced.Publish(Exchange.GetDefault(), queue.Name, false, new Message<Dog>(new Dog()));

            countdownEvent.Wait(1000);
        }
    }
}

// ReSharper restore InconsistentNaming