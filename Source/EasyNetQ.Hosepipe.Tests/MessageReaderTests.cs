// ReSharper disable InconsistentNaming

namespace EasyNetQ.Hosepipe.Tests;

public class MessageReaderTests
{
    private readonly IMessageReader messageReader = new MessageReader();
    private readonly IConventions conventions = new Conventions(new LegacyTypeNameSerializer());

    /// <summary>
    /// 1. Put some messages in C:\temp\MessageOutput
    /// 2. Run this test
    /// 3. Check the output, you should see your messages.
    /// </summary>
    [Fact]
    [Traits.Explicit(@"Needs message files in 'C:\temp\MessageOutput'")]
    public async Task Should_be_able_to_read_messages_from_disk()
    {
        var parameters = new QueueParameters
        {
            MessagesOutputDirectory = @"C:\temp\MessageOutput"
        };

        var messages = messageReader.ReadMessagesAsync(parameters);
        await foreach (var message in messages)
        {
            Console.WriteLine("\nBody:\n{0}\n", message.Body);
            Console.WriteLine("\nProperties:\n{0}\n", message.Properties);
            Console.WriteLine("\nInfo exchange:\n{0}", message.Info.Exchange);
            Console.WriteLine("Info routing key:\n{0}\n", message.Info.RoutingKey);
        }
    }

    [Fact]
    [Traits.Explicit(@"Needs message files in 'C:\temp\MessageOutput'")]
    public async Task Should_be_able_to_read_only_error_messages()
    {
        var parameters = new QueueParameters
        {
            MessagesOutputDirectory = @"C:\temp\MessageOutput"
        };

        var messages = messageReader.ReadMessagesAsync(parameters, conventions.ErrorQueueNamingConvention(default));
        await foreach (var message in messages)
        {
            Console.WriteLine(message.Body);
        }
    }
}

// ReSharper restore InconsistentNaming
