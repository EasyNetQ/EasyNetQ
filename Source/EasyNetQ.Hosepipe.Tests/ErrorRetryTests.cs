// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;

namespace EasyNetQ.Hosepipe.Tests;

public class ErrorRetryTests
{
    private readonly ErrorRetry errorRetry;
    private readonly IConventions conventions;

    public ErrorRetryTests()
    {
        var typeNameSerializer = new LegacyTypeNameSerializer();
        conventions = new Conventions(typeNameSerializer);
        errorRetry = new ErrorRetry(new ReflectionBasedNewtonsoftJsonSerializer(), new DefaultErrorMessageSerializer());
    }

    [Fact]
    [Traits.Explicit("Requires a RabbitMQ instance and messages on disk in the given directory")]
    public async Task Should_republish_all_error_messages_in_the_given_directory()
    {
        var parameters = new QueueParameters
        {
            HostName = "localhost",
            Username = "guest",
            Password = "guest",
            MessagesOutputDirectory = @"C:\temp\MessageOutput"
        };

        var rawErrorMessages = new MessageReader()
           .ReadMessagesAsync(parameters, conventions.ErrorQueueNamingConvention(default));

        await errorRetry.RetryErrorsAsync(rawErrorMessages, parameters);
    }
}

// ReSharper restore InconsistentNaming
