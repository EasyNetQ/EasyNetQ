using EasyNetQ.Consumer;
using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

// As in the AutoSubscriber attribute, a value of 0 is not forwarded, so the consumer keeps the connection-wide concurrency:
// RabbitMQ.Client would start no dispatch worker at all for 0, so the consumer would never handle a message
public sealed class When_consumer_dispatcher_concurrency_is_zero : IAsyncLifetime
{
    private readonly MockBuilder mockBuilder = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_subscription()
    {
        await using var subscription = await mockBuilder.PubSub.SubscribeAsync<MyMessage>(
            "subscription_id",
            _ => { },
            c => c.WithConsumerDispatcherConcurrency(0),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_receiver()
    {
        await using var receiver = await mockBuilder.SendReceive.ReceiveAsync<MyMessage>(
            "my_queue",
            _ => { },
            c => c.WithConsumerDispatcherConcurrency(0),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_responder()
    {
        await using var responder = await mockBuilder.Rpc.RespondAsync<TestRequestMessage, TestResponseMessage>(
            (_, _) => Task.FromResult(new TestResponseMessage(string.Empty)),
            c => c.WithConsumerDispatcherConcurrency(0),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_simple_advanced_consumer_of_typed_messages()
    {
        await using var consumer = await mockBuilder.Bus.Advanced.ConsumeAsync<MyMessage>(
            new Queue("my_queue"),
            (_, _) => Task.CompletedTask,
            c => c.WithConsumerDispatcherConcurrency(0)
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_simple_advanced_consumer_with_handler_registration()
    {
        await using var consumer = await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Queue("my_queue"),
            x => x.Add<MyMessage>((_, _, _) => Task.CompletedTask),
            c => c.WithConsumerDispatcherConcurrency(0)
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    [Fact]
    public async Task Should_keep_the_connection_value_for_a_simple_advanced_consumer_of_raw_messages()
    {
        await using var consumer = await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Queue("my_queue"),
            (_, _, _) => Task.CompletedTask,
            c => c.WithConsumerDispatcherConcurrency(0)
        );

        await ShouldHaveCreatedConsumerChannelWithoutOptionsAsync();
    }

    private async Task ShouldHaveCreatedConsumerChannelWithoutOptionsAsync()
    {
        // Topology and publish channels always pass options; the consumer channel is the only one without them
        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x == null),
            Arg.Any<CancellationToken>()
        );
    }
}
