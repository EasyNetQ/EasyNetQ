using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.Pipeline;

public class ContextInheritanceTests
{
    private static readonly PropertyKey<string> Tenant = new("tenant");

    private readonly ConnectionContext connection;
    private readonly ChannelContext channel;
    private readonly ConsumerContext consumer;
    private readonly ConsumeContext message;

    public ContextInheritanceTests()
    {
        connection = new ConnectionContext("Consumer", new ServiceCollection().BuildServiceProvider());
        channel = new ChannelContext(connection);
        consumer = new ConsumerContext(channel, "queue");
        message = consumer.RentMessageContext();
    }

    [Fact]
    public void Lower_layers_should_read_values_set_on_higher_layers()
    {
        connection.Set(Tenant, "acme");

        channel.TryGet(Tenant, out var fromChannel).Should().BeTrue();
        fromChannel.Should().Be("acme");
        message.TryGet(Tenant, out var fromMessage).Should().BeTrue();
        fromMessage.Should().Be("acme");
        message.Get(Tenant).Should().Be("acme");
    }

    [Fact]
    public void Lower_layers_should_shadow_without_modifying_higher_layers()
    {
        connection.Set(Tenant, "acme");
        message.Set(Tenant, "override");

        message.Get(Tenant).Should().Be("override");
        consumer.Get(Tenant).Should().Be("acme");
        connection.Get(Tenant).Should().Be("acme");
        message.TryGetLocal(Tenant, out _).Should().BeTrue();
        consumer.TryGetLocal(Tenant, out _).Should().BeFalse();
    }

    [Fact]
    public void Views_should_expose_the_hierarchy()
    {
        message.Consumer.Should().BeSameAs(consumer);
        message.Channel.Should().BeSameAs(channel);
        message.Connection.Should().BeSameAs(connection);
        message.Connection.Name.Should().Be("Consumer");
        message.Consumer.Queue.Should().Be("queue");
        message.Parent.Should().BeSameAs(consumer);
    }

    [Fact]
    public void Services_should_be_inherited_and_restored_on_reset()
    {
        message.Services.Should().BeSameAs(connection.Services);

        var scoped = new ServiceCollection().BuildServiceProvider();
        message.Services = scoped;
        message.Services.Should().BeSameAs(scoped);

        consumer.ReturnMessageContext(message);
        consumer.RentMessageContext().Services.Should().BeSameAs(connection.Services);
    }

    [Fact]
    public void GetOrDefault_and_Get_should_behave()
    {
        message.GetOrDefault(Tenant, "none").Should().Be("none");
        message.Contains(Tenant).Should().BeFalse();
        var act = () => message.Get(Tenant);
        act.Should().Throw<KeyNotFoundException>();
    }
}
