using EasyNetQ.AutoSubscribe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.Tests;

public class DefaultMessageConsumerTests
{
    [Fact]
    public void Should_create_consumer_instance_and_consume_message()
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.TryAddSingleton<MyMessageConsumer>();

        var buildServiceProvider = serviceCollection.BuildServiceProvider();

        var consumer = new DefaultAutoSubscriberMessageDispatcher(buildServiceProvider);
        var message = new MyMessage();
        var consumedMessage = (MyMessage)null;

        MyMessageConsumer.ConsumedMessageFunc = m => consumedMessage = m;
        consumer.Dispatch<MyMessage, MyMessageConsumer>(message);

        Assert.Same(message, consumedMessage);
    }

    // Discovered by reflection over test assembly, do not remove.
    private sealed class MyMessageConsumer : IConsume<MyMessage>
    {
        public static Action<MyMessage> ConsumedMessageFunc { get; set; }

        public void Consume(MyMessage message, CancellationToken cancellationToken)
        {
            ConsumedMessageFunc(message);
        }
    }
}
