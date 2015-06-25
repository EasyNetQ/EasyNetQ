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

        [Test,Explicit]
        public void DeclareTopologyAndCheckPassive()
        {
            var queue = advancedBus.QueueDeclare("my_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            advancedBus.Bind(exchange, queue, "routing_key");
            advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct, passive: true);
        }

        [Test, Explicit]
        public void DeclareWithTtlAndExpire()
        {
            advancedBus.QueueDeclare("my_queue", perQueueMessageTtl: 500, expires: 500);
        }

        [Test, Explicit]
        public void DeclareExchangeWithAlternate()
        {
            const string alternate = "alternate";
            const string bindingKey = "the-binding-key";

            var alternateExchange = advancedBus.ExchangeDeclare(alternate, ExchangeType.Direct);
            var originalExchange = advancedBus.ExchangeDeclare("original", ExchangeType.Direct, alternateExchange: alternate);
            var queue = advancedBus.QueueDeclare("my_queue");

            advancedBus.Bind(alternateExchange, queue, bindingKey);

            var message = Encoding.UTF8.GetBytes("Some message");
            advancedBus.Publish(originalExchange, bindingKey, false, false, new MessageProperties(), message);
        }

        [Test, Explicit]
        public void DeclareDelayedExchange()
        {
            const string bindingKey = "the-binding-key";

            var delayedExchange = advancedBus.ExchangeDeclare("delayed", ExchangeType.Direct, delayed: true);
            var queue = advancedBus.QueueDeclare("my_queue");
            advancedBus.Bind(delayedExchange, queue, bindingKey);

            var message = Encoding.UTF8.GetBytes("Some message");
            var messageProperties = new MessageProperties();
            messageProperties.Headers.Add("x-delay", 5000);
            advancedBus.Publish(delayedExchange, bindingKey, false, false, messageProperties, message);
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

            var body = Encoding.UTF8.GetBytes("Hello World!");
            advancedBus.Publish(exchange, "routing_key", false, false, new MessageProperties(), body);

            Thread.Sleep(5000);
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

        [Test, Explicit]
        public void Should_consume_a_message()
        {
            var queue = advancedBus.QueueDeclare("consume_test");
            advancedBus.Consume<MyMessage>(queue, (message, info) => 
                Task.Factory.StartNew(() => 
                    Console.WriteLine("Got message {0}", message.Body.Text)));

            advancedBus.Publish(Exchange.GetDefault(), "consume_test", false, false, 
                new Message<MyMessage>(new MyMessage{ Text = "Wotcha!"}));

            Thread.Sleep(1000);
        }

        [Test, Explicit]
        public void Should_be_able_to_get_a_message()
        {
            var queue = advancedBus.QueueDeclare("get_test");
            advancedBus.Publish(Exchange.GetDefault(), "get_test", false, false,
                new Message<MyMessage>(new MyMessage { Text = "Oh! Hello!" }));

            var getResult = advancedBus.Get<MyMessage>(queue);

            if (getResult.MessageAvailable)
            {
                Console.Out.WriteLine("Got message: {0}", getResult.Message.Body.Text);
            }
            else
            {
                Console.Out.WriteLine("Failed to get message!");
            }
        }

        [Test, Explicit]
        public void Should_set_MessageAvailable_to_false_when_queue_is_empty()
        {
            var queue = advancedBus.QueueDeclare("get_empty_queue_test");
            var getResult = advancedBus.Get<MyMessage>(queue);

            if (!getResult.MessageAvailable)
            {
                Console.Out.WriteLine("Failed to get message!");
            }
        }

        [Test, Explicit]
        public void Should_be_able_to_get_queue_length()
        {
            var queue = advancedBus.QueueDeclare("count_test");
            advancedBus.Publish(Exchange.GetDefault(), "count_test", false, false,
                new Message<MyMessage>(new MyMessage { Text = "Oh! Hello!" }));
            uint messageCount = advancedBus.MessageCount(queue);
            Console.WriteLine("{0} messages in queue", messageCount);
        }
    }
}

// ReSharper restore InconsistentNaming