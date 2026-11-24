using EasyNetQ.Configuration;

namespace EasyNetQ.Tests.FluentConfiguration;

public class When_configuring_a_rabbitmq_consumer
{
    [Fact]
    public void Should_set_consumer_dispatcher_concurrency_on_the_consumer_context()
    {
        var builder = new RabbitMqConsumerBuilder(new ConsumerDefinition());
        var context = TestContexts.Consumer();

        builder.ConsumerDispatcherConcurrency(8);
        builder.Definition.ConfigureContext?.Invoke(context);

        context.ConsumerDispatcherConcurrency.Should().Be(8);
    }

    [Fact]
    public void Should_leave_consumer_dispatcher_concurrency_to_the_connection_by_default()
    {
        var builder = new RabbitMqConsumerBuilder(new ConsumerDefinition());
        var context = TestContexts.Consumer();

        builder.Definition.ConfigureContext?.Invoke(context);

        context.ConsumerDispatcherConcurrency.Should().BeNull();
    }

    [Fact]
    public void Should_reject_zero_consumer_dispatcher_concurrency()
    {
        var builder = new RabbitMqConsumerBuilder(new ConsumerDefinition());

        var act = () => builder.ConsumerDispatcherConcurrency(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
