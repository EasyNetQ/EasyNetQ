// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using Xunit;

namespace EasyNetQ.Hosepipe.Tests;

public class ErrorRetryTests
{
    private readonly ErrorRetry errorRetry;

    public ErrorRetryTests()
    {
        errorRetry = new ErrorRetry(new JsonSerializer(), new DefaultErrorMessageSerializer());
    }

    [Fact]
    [Traits.Explicit("Requires a RabbitMQ instance and messages on disk in the given directory")]
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
            .ReadMessages(parameters, "EasyNetQ_Default_Error_Queue");

        errorRetry.RetryErrors(rawErrorMessages, parameters);
    }
}

// ReSharper restore InconsistentNaming
