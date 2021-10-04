// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class QueueRetrievalTests
    {
        [Fact]
        [Traits.Explicit("Requires a RabbitMQ server on localhost")]
        public void TryGetMessagesFromQueue()
        {
            const string queue = "EasyNetQ_Hosepipe_Tests_QueueRetrievalTests+TestMessage:EasyNetQ_Hosepipe_Tests_hosepipe";

            var queueRetrieval = new QueueRetrieval(new DefaultErrorMessageSerializer());
            var parameters = new QueueParameters
            {
                QueueName = queue,
                Purge = false
            };

            foreach (var message in queueRetrieval.GetMessagesFromQueue(parameters))
            {
                Console.Out.WriteLine("message:\n{0}", message.Body);
                Console.Out.WriteLine("properties correlation id:\n{0}", message.Properties.CorrelationId);
                Console.Out.WriteLine("info exchange:\n{0}", message.Info.Exchange);
                Console.Out.WriteLine("info routing key:\n{0}", message.Info.RoutingKey);
            }
        }

        [Fact]
        [Traits.Explicit("Requires a RabbitMQ server on localhost")]
        public async Task PublishSomeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            for (var i = 0; i < 10; i++)
            {
                await bus.PubSub.PublishAsync(new TestMessage { Text = string.Format("\n>>>>>> Message {0}\n", i) });
            }

            bus.Dispose();
        }

        [Fact]
        [Traits.Explicit("Requires a RabbitMQ server on localhost")]
        public async Task ConsumeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            await bus.PubSub.SubscribeAsync<TestMessage>("hosepipe", message => Console.WriteLine(message.Text));

            Thread.Sleep(1000);

            bus.Dispose();
        }

        private class TestMessage
        {
            public string Text { get; set; }
        }
    }
}

// ReSharper restore InconsistentNaming
