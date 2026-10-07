using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;

namespace EasyNetQ.Tests.ConsumeTests;

// RabbitMQ.Client would start no dispatch workers at all for zero, so the consumer would silently never handle a message
public class When_consumer_dispatcher_concurrency_is_invalid
{
    public static IEnumerable<object[]> Configurations =>
        new List<object[]>
        {
            new object[] { nameof(IConsumeConfiguration) },
            new object[] { nameof(ISimpleConsumeConfiguration) },
            new object[] { nameof(ISubscriptionConfiguration) },
            new object[] { nameof(IReceiveConfiguration) },
            new object[] { nameof(IResponderConfiguration) }
        };

    [Fact]
    public void Should_reject_zero_for_a_consume_configuration()
    {
        var configuration = new ConsumeConfiguration(50, Substitute.For<IHandlerCollectionFactory>());

        var act = () => configuration.WithConsumerDispatcherConcurrency(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
        configuration.ConsumerDispatcherConcurrency.Should().BeNull();
    }

    [Fact]
    public async Task Should_reject_zero_for_a_simple_consume_configuration()
    {
        await using var mockBuilder = new MockBuilder();

        var act = () => mockBuilder.Bus.Advanced.ConsumeAsync(
            new Queue("my_queue"),
            (_, _, _) => Task.CompletedTask,
            c => c.WithConsumerDispatcherConcurrency(0)
        );

        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
        mockBuilder.Consumers.Should().BeEmpty();
    }

    [Fact]
    public void Should_reject_zero_for_a_subscription_configuration()
    {
        var configuration = new SubscriptionConfiguration(50);

        var act = () => configuration.WithConsumerDispatcherConcurrency(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
        configuration.ConsumerDispatcherConcurrency.Should().BeNull();
    }

    [Fact]
    public void Should_reject_zero_for_a_receive_configuration()
    {
        var configuration = new ReceiveConfiguration(50);

        var act = () => configuration.WithConsumerDispatcherConcurrency(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
        configuration.ConsumerDispatcherConcurrency.Should().BeNull();
    }

    [Fact]
    public void Should_reject_zero_for_a_responder_configuration()
    {
        var configuration = new ResponderConfiguration(50);

        var act = () => configuration.WithConsumerDispatcherConcurrency(0);

        act.Should().Throw<ArgumentOutOfRangeException>();
        configuration.ConsumerDispatcherConcurrency.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void Should_reject_a_configuration_not_created_by_easynetq(string configuration)
    {
        var act = () => WithConsumerDispatcherConcurrencyOnSubstitute(configuration, 3);

        act.Should().Throw<NotSupportedException>();
    }

    private static void WithConsumerDispatcherConcurrencyOnSubstitute(string configuration, ushort consumerDispatcherConcurrency)
    {
        switch (configuration)
        {
            case nameof(IConsumeConfiguration):
                Substitute.For<IConsumeConfiguration>().WithConsumerDispatcherConcurrency(consumerDispatcherConcurrency);
                break;
            case nameof(ISimpleConsumeConfiguration):
                Substitute.For<ISimpleConsumeConfiguration>().WithConsumerDispatcherConcurrency(consumerDispatcherConcurrency);
                break;
            case nameof(ISubscriptionConfiguration):
                Substitute.For<ISubscriptionConfiguration>().WithConsumerDispatcherConcurrency(consumerDispatcherConcurrency);
                break;
            case nameof(IReceiveConfiguration):
                Substitute.For<IReceiveConfiguration>().WithConsumerDispatcherConcurrency(consumerDispatcherConcurrency);
                break;
            case nameof(IResponderConfiguration):
                Substitute.For<IResponderConfiguration>().WithConsumerDispatcherConcurrency(consumerDispatcherConcurrency);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(configuration), configuration, null);
        }
    }
}
