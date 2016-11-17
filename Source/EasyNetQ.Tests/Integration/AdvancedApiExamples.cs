// ReSharper disable InconsistentNaming

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;
using Xunit;

namespace EasyNetQ.Tests.Integration
{
    [Explicit("Requires a RabbitMQ instance on localhost")]
    public class AdvancedApiExamples : IDisposable
    {
        private IAdvancedBus advancedBus;

        public AdvancedApiExamples()
        {
            advancedBus = RabbitHutch.CreateBus("host=localhost").Advanced;
        }

        public void Dispose()
        {
            advancedBus.Dispose();
        }

        [Fact][Explicit]
        public void DeclareTopology()
        {
            var queue = advancedBus.QueueDeclare("my_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            advancedBus.Bind(exchange, queue, "routing_key");

        }

        [Fact][Explicit]
        public void DeclareTopologyAndCheckPassive()
        {
            var queue = advancedBus.QueueDeclare("my_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct);
            advancedBus.Bind(exchange, queue, "routing_key");
            advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Direct, passive: true);
        }

        [Fact][Explicit]
        public void DeclareWithTtlAndExpire()
        {
            advancedBus.QueueDeclare("my_queue", perQueueMessageTtl: 500, expires: 500);
        }

        [Fact][Explicit]
        public void DeclareExchangeWithAlternate()
        {
            const string alternate = "alternate";
            const string bindingKey = "the-binding-key";

            var alternateExchange = advancedBus.ExchangeDeclare(alternate, ExchangeType.Direct);
            var originalExchange = advancedBus.ExchangeDeclare("original", ExchangeType.Direct, alternateExchange: alternate);
            var queue = advancedBus.QueueDeclare("my_queue");

            advancedBus.Bind(alternateExchange, queue, bindingKey);

            var message = Encoding.UTF8.GetBytes("Some message");
            advancedBus.Publish(originalExchange, bindingKey, false, new MessageProperties(), message);
        }

        [Fact][Explicit]
        public void DeclareDelayedExchange()
        {
            const string bindingKey = "the-binding-key";

            var delayedExchange = advancedBus.ExchangeDeclare("delayed", ExchangeType.Direct, delayed: true);
            var queue = advancedBus.QueueDeclare("my_queue");
            advancedBus.Bind(delayedExchange, queue, bindingKey);

            var message = Encoding.UTF8.GetBytes("Some message");
            var messageProperties = new MessageProperties();
            messageProperties.Headers.Add("x-delay", 5000);
            advancedBus.Publish(delayedExchange, bindingKey, false, messageProperties, message);
        }


        [Fact][Explicit]
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

        [Fact][Explicit]
        public void PublishToAnExchange()
        {
            var exchange = new Exchange("my_exchange");

            var body = Encoding.UTF8.GetBytes("Hello World!");
            advancedBus.Publish(exchange, "routing_key", false, new MessageProperties(), body);

            Thread.Sleep(5000);
        }

        [Fact][Explicit]
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

        [Fact][Explicit]
        public void Should_consume_a_message()
        {
            var queue = advancedBus.QueueDeclare("consume_test");
            advancedBus.Consume<MyMessage>(queue, (message, info) => 
                Task.Factory.StartNew(() => 
                    Console.WriteLine("Got message {0}", message.Body.Text)));

            advancedBus.Publish(Exchange.GetDefault(), "consume_test", false, new Message<MyMessage>(new MyMessage{ Text = "Wotcha!"}));

            Thread.Sleep(1000);
        }

        [Fact][Explicit]
        public void Should_be_able_to_get_a_message()
        {
            var queue = advancedBus.QueueDeclare("get_test");
            advancedBus.Publish(Exchange.GetDefault(), "get_test", false, new Message<MyMessage>(new MyMessage { Text = "Oh! Hello!" }));

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

        [Fact][Explicit]
        public void Should_set_MessageAvailable_to_false_when_queue_is_empty()
        {
            var queue = advancedBus.QueueDeclare("get_empty_queue_test");
            var getResult = advancedBus.Get<MyMessage>(queue);

            if (!getResult.MessageAvailable)
            {
                Console.Out.WriteLine("Failed to get message!");
            }
        }

        [Fact][Explicit]
        public void Should_be_able_to_get_queue_length()
        {
            var queue = advancedBus.QueueDeclare("count_test");
            advancedBus.Publish(Exchange.GetDefault(), "count_test", false, new Message<MyMessage>(new MyMessage { Text = "Oh! Hello!" }));
            uint messageCount = advancedBus.MessageCount(queue);
            Console.WriteLine("{0} messages in queue", messageCount);
        }

        [Fact][Explicit]
        public void Should_be_able_to_dead_letter_to_fixed_queue()
        {
            // create a main queue and a retry queue with retry queue dead lettering messages directly
            // to main queue
            var queue = advancedBus.QueueDeclare("main_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Topic);
            var retryQueue = advancedBus.QueueDeclare("retry_queue", deadLetterExchange: "", deadLetterRoutingKey: "main_queue");
            advancedBus.Bind(exchange, retryQueue, "routing_key");

            // consume from main queue to see if dead lettering is working as expected
            advancedBus.Consume<MyMessage>(queue, (message, info) =>
                Task.Factory.StartNew(() =>
                    Console.WriteLine("Got message {0}", message.Body.Text)));

            // publish the message to retry queue which should end up in the main queue after expiration
            advancedBus.Publish(exchange, "routing_key", false, new Message<MyMessage>(new MyMessage() { Text = "My Message" }, new MessageProperties { Expiration = "50" }));

            Thread.Sleep(1000);
        }

        [Fact][Explicit]
        public void Should_be_able_to_dead_letter_to_given_exchange()
        {
            // create a main queue and a retry queue both binding to the same topic exchange with 
            // different routing keys. Retry queue is dead lettering to the exchange with routing key
            // of main queue binding.
            var queue = advancedBus.QueueDeclare("main_queue");
            var exchange = advancedBus.ExchangeDeclare("my_exchange", ExchangeType.Topic);
            advancedBus.Bind(exchange, queue, "main_routing_key");
            var retryQueue = advancedBus.QueueDeclare("my_retry_queue", deadLetterExchange: "my_exchange", deadLetterRoutingKey: "main_routing_key");
            advancedBus.Bind(exchange, retryQueue, "retry_routing_key");

            // consume messages from main queue
            advancedBus.Consume<MyMessage>(queue, (message, info) =>
                Task.Factory.StartNew(() =>
                    Console.WriteLine("Got message {0}", message.Body.Text)));

            // publish message to the retry queue which should dead letter to main queue after expiration
            advancedBus.Publish(exchange, "retry_routing_key", false, new Message<MyMessage>(new MyMessage() { Text = "My Message" }, new MessageProperties { Expiration = "50" }));

            Thread.Sleep(1000);
        }
    }
}

// ReSharper restore InconsistentNaming