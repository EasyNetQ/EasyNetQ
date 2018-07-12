// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;
using EasyNetQ.Tests;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests
{
    public class ErrorRetryTests
    {
        private ErrorRetry errorRetry;
        private IConventions conventions;

        public ErrorRetryTests()
        {
            var typeNameSerializer = new LegacyTypeNameSerializer();
            conventions = new Conventions(typeNameSerializer);
            errorRetry = new ErrorRetry(new JsonSerializer(), new DefaultErrorMessageSerializer());
        }

        [Fact][Explicit("Requires a RabbitMQ instance and messages on disk in the given directory")]
        public void Should_republish_all_error_messages_in_the_given_directory()
        {
            var parameters = new QueueParameters
            {
                HostName = "localhost",
                Username = "guest",
                Password = "guest",
                MessagesOutputDirectory = @"C:\temp\MessageOutput"
            };

            var rawErrorMessages = new MessageReader()
                .ReadMessages(parameters, conventions.ErrorQueueNamingConvention(new MessageReceivedInfo()));

            errorRetry.RetryErrors(rawErrorMessages, parameters);
        }

        [Fact][Explicit("Requires a RabbitMQ instance")]
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
                MessagesOutputDirectory = @"C:\temp\MessageOutput"
            };

            errorRetry.RepublishError(error, parameters);
        }
    }
}

// ReSharper restore InconsistentNaming