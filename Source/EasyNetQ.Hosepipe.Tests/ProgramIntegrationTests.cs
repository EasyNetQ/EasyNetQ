using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Hosepipe.Tests;

[Traits.Explicit(@"Requires a RabbitMQ broker on localhost and access to C:\Temp\MessageOutput")]
public class ProgramIntegrationTests
{
    private const string outputPath = @"C:\Temp\MessageOutput";
    private const string queue = "EasyNetQ_Hosepipe_Tests_ProgramIntegrationTests+TestMessage:EasyNetQ_Hosepipe_Tests_hosepipe";

    public async Task DumpMessagesAsync()
    {
        ClearDirectory();

        var args = new[]
        {
            "dump",
            "s:localhost",
            string.Format("q:{0}", queue),
            string.Format("o:{0}", outputPath)
        };

        await Program.Main(args);

        ListDirectory();
    }

    public async Task InsertMessagesAsync()
    {
        var args = new[]
        {
            "insert",
            "s:localhost",
            string.Format("o:{0}", outputPath)
        };

        await Program.Main(args);
    }

    public void ListDirectory()
    {
        foreach (var file in Directory.GetFiles(outputPath))
        {
            Console.Out.WriteLine(file);
            Console.Out.WriteLine(File.ReadAllText(file));
            Console.Out.WriteLine("");
        }
    }

    public void ClearDirectory()
    {
        foreach (var file in Directory.GetFiles(outputPath))
        {
            File.Delete(file);
        }
    }

    public void PublishSomeMessages()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ("host=localhost");
        using var provider = serviceCollection.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();
        for (var i = 0; i < 10; i++)
        {
            bus.PubSub.Publish(new TestMessage { Text = string.Format("\n>>>>>> Message {0}\n", i) });
        }
    }

    public async Task ConsumeMessagesAsync()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ("host=localhost");

        await using var provider = serviceCollection.BuildServiceProvider();
        var bus = provider.GetRequiredService<IBus>();
        await using var subscription = await bus.PubSub.SubscribeAsync<TestMessage>("hosepipe", message => Console.WriteLine(message.Text));
        await Task.Delay(1000);
    }

    private sealed class TestMessage
    {
        public string Text { get; set; }
    }
}
