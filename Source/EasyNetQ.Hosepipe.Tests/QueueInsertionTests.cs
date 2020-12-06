// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class QueueInsertionTests
    {
        private readonly IQueueInsertion queueInsertion;

        public QueueInsertionTests()
        {
            queueInsertion = new QueueInsertion(new DefaultErrorMessageSerializer());
        }

        /// <summary>
        /// Create a RabbitMQ queue 'Hosepipe_test_queue' before attempting this test.
        /// </summary>
        [Fact][Traits.Explicit("Needs a RabbitMQ server on localhost")]
        public void Should_be_able_to_inset_messages_onto_a_queue()
        {
            var messages = new[]
            {
                new HosepipeMessage("{\"Text\":\"I am message one\"}", new MessageProperties(), Helper.CreateMessageReceivedInfo()),
                new HosepipeMessage("{\"Text\":\"I am message two\"}", new MessageProperties(), Helper.CreateMessageReceivedInfo())
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
