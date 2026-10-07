using EasyNetQ.Tests.Mocking;
using EasyNetQ.Topology;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public sealed class When_consumer_dispatcher_concurrency_is_configured : IAsyncLifetime
{
    private const ushort ConsumerDispatcherConcurrency = 3;

    private readonly MockBuilder mockBuilder = new();

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await mockBuilder.DisposeAsync();
    }

    [Fact]
    public async Task Should_set_it_on_the_channel_of_an_advanced_consumer()
    {
        await using var consumer = await mockBuilder.Bus.Advanced.ConsumeAsync(
            c => c.WithConsumerDispatcherConcurrency(ConsumerDispatcherConcurrency)
                .ForQueue(new Queue("my_queue"), (_, _, _, _) => Task.CompletedTask)
        );

        await ShouldHaveCreatedConsumerChannelWithConcurrencyAsync();
    }

    [Fact]
    public async Task Should_set_it_on_the_channel_of_a_simple_advanced_consumer()
    {
        await using var consumer = await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Queue("my_queue"),
            (_, _, _) => Task.CompletedTask,
            c => c.WithConsumerDispatcherConcurrency(ConsumerDispatcherConcurrency)
        );

        await ShouldHaveCreatedConsumerChannelWithConcurrencyAsync();
    }

    [Fact]
    public async Task Should_set_it_on_the_channel_of_a_subscription()
    {
        await using var subscription = await mockBuilder.PubSub.SubscribeAsync<MyMessage>(
            "subscription_id",
            _ => { },
            c => c.WithConsumerDispatcherConcurrency(ConsumerDispatcherConcurrency),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithConcurrencyAsync();
    }

    [Fact]
    public async Task Should_set_it_on_the_channel_of_a_receiver()
    {
        await using var receiver = await mockBuilder.SendReceive.ReceiveAsync<MyMessage>(
            "my_queue",
            _ => { },
            c => c.WithConsumerDispatcherConcurrency(ConsumerDispatcherConcurrency),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithConcurrencyAsync();
    }

    [Fact]
    public async Task Should_set_it_on_the_channel_of_a_responder()
    {
        await using var responder = await mockBuilder.Rpc.RespondAsync<TestRequestMessage, TestResponseMessage>(
            (_, _) => Task.FromResult(new TestResponseMessage(string.Empty)),
            c => c.WithConsumerDispatcherConcurrency(ConsumerDispatcherConcurrency),
            TestContext.Current.CancellationToken
        );

        await ShouldHaveCreatedConsumerChannelWithConcurrencyAsync();
    }

    [Fact]
    public async Task Should_not_pass_channel_options_for_a_subscription_without_it()
    {
        await using var subscription = await mockBuilder.PubSub.SubscribeAsync<MyMessage>("subscription_id", _ => { }, TestContext.Current.CancellationToken);

        // Topology and publish channels always pass options; the consumer channel is the only one without them
        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x == null),
            Arg.Any<CancellationToken>()
        );
    }

    private async Task ShouldHaveCreatedConsumerChannelWithConcurrencyAsync()
    {
        await mockBuilder.Connection.Received(1).CreateChannelAsync(
            Arg.Is<CreateChannelOptions>(x => x != null && x.ConsumerDispatchConcurrency == ConsumerDispatcherConcurrency),
            Arg.Any<CancellationToken>()
        );
    }
}
