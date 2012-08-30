// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Text;
using EasyNetQ.Loggers;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Bugs
{
    [TestFixture]
    public class CannonCreateQueueExchangeUsingAdvancedAPI
    {
        [SetUp]
        public void SetUp()
        {
        }

        /// <summary>
        /// User expected the queue and binding to be declared, but Publish merely declares the exchange.
        /// This works as expected, although it's arguable that the advanced API should separate declares
        /// from publish and consume.
        /// </summary>
        [Test, Explicit("Requires a local RabbitMQ instance")]
        public void Should_create_queue_and_exchange()
        {
            var bus = RabbitHutch.CreateBus("host=localhost", new ConsoleLogger()).Advanced;
            var queue = Queue.Declare(true, false, false, "testq2", new Dictionary<string, object>() { { "x-ha-policy", "all" } });
            var exchange = Exchange.DeclareTopic("exchamgename");
            queue.BindTo(exchange, "test");
            
            using (var publishChannel = bus.OpenPublishChannel())
            {
                publishChannel.Publish(
                    exchange, "test",
                    new MessageProperties() { DeliveryMode = 2 },
                    Encoding.UTF8.GetBytes("Hello World"));
            }
                
            bus.Dispose();
        }
    }
}

// ReSharper restore InconsistentNaming