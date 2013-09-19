// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using NUnit.Framework;

namespace EasyNetQ.Tests.Integration
{
    [TestFixture, Explicit("Requires a RabbitMQ instance on localhost")]
    public class AdvancedApiExamples
    {
        private IAdvancedBus advancedBus;

        [SetUp]
        public void SetUp()
        {
            advancedBus = RabbitHutch.CreateBus("host=localhost").Advanced;
        }

        [TearDown]
        public void TearDown()
        {
            advancedBus.Dispose();
        }

        [Test, Explicit]
        public void DeclareTopology()
        {
            var queue = advancedBus.QueueDeclare("my_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            advancedBus.Bind(exchange, queue, "routing_key");

        }

        [Test, Explicit]
        public void ConsumeFromAQueue()
        {
            var queue = new Queue("my_queue", false);
            advancedBus.Consume(queue, (body, properties, info) => Task.Factory.StartNew(() =>
                {
                    var message = Encoding.UTF8.GetString(body);
                    Console.Out.WriteLine("Got message: '{0}'", message);
                }));

            Thread.Sleep(500);
        }

        [Test, Explicit]
        public void PublishToAnExchange()
        {
            var exchange = new Exchange("my_exchange");

            using (var channel = advancedBus.OpenPublishChannel())
            {
                var body = Encoding.UTF8.GetBytes("Hello World!");
                channel.Publish(exchange, "routing_key", new MessageProperties(), body);
            }

            Thread.Sleep(500);
        }

        [Test, Explicit]
        public void Should_be_able_to_delete_objects()
        {
            // declare some objects
            var queue = advancedBus.QueueDeclare("my_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            var binding = advancedBus.Bind(exchange, queue, "routing_key");

            // and then delete them
            advancedBus.BindingDelete(binding);
            advancedBus.ExchangeDelete(exchange);
            advancedBus.QueueDelete(queue);
        }
    }
}

// ReSharper restore InconsistentNaming