// ReSharper disable InconsistentNaming

using System;
using System.Threading;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class QueueRetrievalTests
    {
        [SetUp]
        public void SetUp() {}

        [Test, Explicit("Requires a RabbitMQ server on localhost")]
        public void TryGetMessagesFromQueue()
        {
            const string queue = "EasyNetQ_Hosepipe_Tests_QueueRetrievalTests+TestMessage:EasyNetQ_Hosepipe_Tests_hosepipe";

            var queueRetrieval = new QueueRetreival();
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

        [Test, Explicit("Requires a RabbitMQ server on localhost")]
        public void PublishSomeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            for (int i = 0; i < 10; i++)
            {
                bus.Publish(new TestMessage{ Text = string.Format("\n>>>>>> Message {0}\n", i)});
            }

            bus.Dispose();
        }

        public void ConsumeMessages()
        {
            var bus = RabbitHutch.CreateBus("host=localhost");

            bus.Subscribe<TestMessage>("hosepipe", message => Console.WriteLine(message.Text));

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