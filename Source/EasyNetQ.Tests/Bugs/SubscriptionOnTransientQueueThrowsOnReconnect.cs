// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Tests.Integration;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Bugs
{
    [TestFixture]
    public class SubscriptionOnTransientQueueThrowsOnReconnect
    {
        private IAdvancedBus bus;
        private const string routingKey = "routing.key";

        [SetUp]
        public void SetUp()
        {
            bus = RabbitHutch.CreateBus("host=localhost").Advanced;
        }

        [TearDown]
        public void TearDown()
        {
            bus.Dispose();
        }

        [Test, Explicit("Needs a rabbitMQ server on localhost to run")]
        public void Should_not_throw_on_bus_reconnect()
        {
            var queue = Queue.DeclareTransient();

            bus.Subscribe<MyMessage>(queue, (message, info) => Task.Factory.StartNew(() => Console.WriteLine("Got message: {0}", message.Body.Text)));

            // now, force close the connection

            Thread.Sleep(TimeSpan.FromMinutes(2));
        }
    }
}

// ReSharper restore InconsistentNaming