// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    // Expect to see:
    // ==============
    // INFO: Basic ack failed because channel was closed with message 
    // 'The AMQP operation was interrupted: AMQP close-reason, initiated by Application, 
    // code=200, text="Goodbye", classId=0, methodId=0, cause='. 
    // Message remains on RabbitMQ and will be retried. 
    // ConsumerTag: 36c2b0c5-11c9-4f4e-b3f8-9943d113e646, DeliveryTag: 1
    [Explicit("Requires a RabbitMQ broker on localhost")]
    public class ConsumerShutdownTests
    {
        [Fact]
        public void Message_can_be_processed_but_not_ACKd()
        {
            var waitTime = TimeSpan.FromMinutes(2);
            var receivedEvent = new AutoResetEvent(false);
            var processedEvent = new AutoResetEvent(false);

            var bus = RabbitHutch.CreateBus("host=localhost");
            const string queueName = "shutdown_test";
            var message = Encoding.UTF8.GetBytes("Hello There!");

            var queue = bus.Advanced.QueueDeclare(queueName);
            bus.Advanced.QueuePurge(queue);

            bus.Advanced.Consume(queue, (body, properties, info) =>
                {
                    // signal that message has been received (but not completed)
                    receivedEvent.Set();
                    return Task.Factory.StartNew(() =>
                        {
                            Console.Out.WriteLine("Started processing.");
                            Thread.Sleep(500);
                            var receivedMessage = Encoding.UTF8.GetString(body);
                            Console.Out.WriteLine("Completed: {0}", receivedMessage);
                            processedEvent.Set();
                        });
                });

            bus.Advanced.Publish(Exchange.GetDefault(), queueName, false, new MessageProperties(), message);
            Console.Out.WriteLine("Published");

            var signalReceived = receivedEvent.WaitOne(waitTime);
            Assert.True(signalReceived, $"Expected reset event within {waitTime.TotalSeconds} seconds");

            Console.Out.WriteLine("Dispose Called");
            bus.Dispose();
            Console.Out.WriteLine("Disposed");

            signalReceived = processedEvent.WaitOne(waitTime);
            Assert.True(signalReceived, $"Expected reset event within {waitTime.TotalSeconds} seconds");
        }
    }
}

// ReSharper restore InconsistentNaming