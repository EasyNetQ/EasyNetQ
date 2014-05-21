// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Management.Client;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture]
    [Explicit("Requires a RabbitMQ on localhost")]
    public class DeleteQueueWhileConsuming
    {
        private IBus bus;
        private const string queueName = "queue_to_delete";

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
        public void Start_consuming_then_delete_a_queue()
        {
            var queue = bus.Advanced.QueueDeclare(queueName);

            bus.Advanced.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    Console.Out.WriteLine("Got message");
                }));

            DeleteQueue(queueName);

            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        private static void DeleteQueue(string queueToDelete)
        {
            var management = new ManagementClient("http://localhost", "guest", "guest");

            var vhost = management.GetVhost("/");
            var queue = management.GetQueue(queueToDelete, vhost);
            management.DeleteQueue(queue);
        }
    }
}

// ReSharper restore InconsistentNaming