// ReSharper disable InconsistentNaming

using EasyNetQ.SystemMessages;
using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class ErrorRetryTests
    {
        private ErrorRetry errorRetry;
        private IConventions conventions;

        [SetUp]
        public void SetUp()
        {
            var typeNameSerializer = new TypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
            errorRetry = new ErrorRetry(new JsonSerializer(typeNameSerializer));
        }

        [Test, Explicit("Requires a RabbitMQ instance and messages on disk in the given directory")]
        public void Should_republish_all_error_messages_in_the_given_directory()
        {
            var parameters = new QueueParameters
            {
                HostName = "localhost",
                Username = "guest",
                Password = "guest",
                MessageFilePath = @"C:\temp\MessageOutput"
            };

            var rawErrorMessages = new MessageReader()
                .ReadMessages(parameters, conventions.ErrorQueueNamingConvention());

            errorRetry.RetryErrors(rawErrorMessages, parameters);
        }

        [Test, Explicit("Requires a RabbitMQ instance")]
        public void Should_republish_to_default_exchange()
        {
            var error = new Error
                {
                    Exchange = "", // default exchange
                    RoutingKey = "hosepipe.test",
                    Message = "Hosepipe test message",
                    BasicProperties = new MessageProperties()
                };
            var parameters = new QueueParameters
            {
                HostName = "localhost",
                Username = "guest",
                Password = "guest",
                MessageFilePath = @"C:\temp\MessageOutput"
            };

            errorRetry.RepublishError(error, parameters);
        }
    }
}

// ReSharper restore InconsistentNaming