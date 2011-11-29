// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class QueueInsertionTests
    {
        private IQueueInsertion queueInsertion;

        [SetUp]
        public void SetUp()
        {
            queueInsertion = new QueueInsertion();
        }

        /// <summary>
        /// Create a RabbitMQ queue 'Hosepipe_test_queue' before attempting this test.
        /// </summary>
        [Test, Explicit("Needs a RabbitMQ server on localhost")]
        public void Should_be_able_to_inset_messages_onto_a_queue()
        {
            var messages = new[]
            {
                "{\"Text\":\"I am message one\"}",
                "{\"Text\":\"I am message two\"}"
            };

            var parameters = new QueueParameters
            {
                HostName = "localhost",
                QueueName = "Hosepipe_test_queue"
            };

            queueInsertion.PublishMessagesToQueue(messages, parameters);
        }
    }
}

// ReSharper restore InconsistentNaming