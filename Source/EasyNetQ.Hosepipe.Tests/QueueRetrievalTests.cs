// ReSharper disable InconsistentNaming

using EasyNetQ.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Hosepipe.Tests;

public class QueueRetrievalTests
{
    [Fact]
    [Traits.Explicit("Requires a RabbitMQ server on localhost")]
    public async Task TryGetMessagesFromQueueAsync()
    {
        const string queue = "EasyNetQ_Hosepipe_Tests_QueueRetrievalTests+TestMessage:EasyNetQ_Hosepipe_Tests_hosepipe";

        var queueRetrieval = new QueueRetrieval(new DefaultErrorMessageSerializer());
        var parameters = new QueueParameters
        {
            QueueName = queue,
            Purge = false
        };

        await foreach (var message in queueRetrieval.GetMessagesFromQueueAsync(parameters))
        {
            Console.Out.WriteLine("message:\n{0}", message.Body);
            Console.Out.WriteLine("properties correlation id:\n{0}", message.Properties.CorrelationId);
            Console.Out.WriteLine("info exchange:\n{0}", message.Info.Exchange);
            Console.Out.WriteLine("info routing key:\n{0}", message.Info.RoutingKey);
        }
    }

    [Fact]
    [Traits.Explicit("Requires a RabbitMQ server on localhost")]
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

    [Fact]
    [Traits.Explicit("Requires a RabbitMQ server on localhost")]
    public void ConsumeMessages()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ("host=localhost");

        using var provider = serviceCollection.BuildServiceProvider();

        var bus = provider.GetRequiredService<IBus>();
        using var subscription = bus.PubSub.Subscribe<TestMessage>("hosepipe", message => Console.WriteLine(message.Text));


        Thread.Sleep(1000);
    }

    private sealed class TestMessage
    {
        public string Text { get; set; }
    }
}

// ReSharper restore InconsistentNaming
