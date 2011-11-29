// ReSharper disable InconsistentNaming

using System;
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
            const string queue = "EasyNetQ_Default_Error_Queue";

            var queueRetrieval = new QueueRetreival();
            var parameters = new QueueParameters
            {
                QueueName = queue,
                Purge = false
            };

            foreach (var message in queueRetrieval.GetMessagesFromQueue(parameters))
            {
                Console.Out.WriteLine("message = {0}", message);
            }
        }
         
    }
}

// ReSharper restore InconsistentNaming