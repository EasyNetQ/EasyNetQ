// ReSharper disable InconsistentNaming

using NUnit.Framework;

namespace EasyNetQ.Hosepipe.Tests
{
    [TestFixture]
    public class ErrorRetryTests
    {
        private ErrorRetry errorRetry;

        [SetUp]
        public void SetUp()
        {
            errorRetry = new ErrorRetry(new JsonSerializer());
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
                .ReadMessages(parameters, DefaultConsumerErrorStrategy.EasyNetQErrorQueue);

            errorRetry.RetryErrors(rawErrorMessages, parameters);
        }
    }
}

// ReSharper restore InconsistentNaming