using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests;

public sealed class ModelCleanupTests : IAsyncLifetime
{
    private readonly IBus bus;
    private readonly MockBuilder mockBuilder;
    private readonly TimeSpan waitTime;
    private bool disposed;

    public ModelCleanupTests()
    {
        mockBuilder = new MockBuilder();
        bus = mockBuilder.Bus;
        waitTime = TimeSpan.FromSeconds(10);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        if (disposed)
            return;

        disposed = true;
        await mockBuilder.DisposeAsync();
    }

    private AutoResetEvent WaitForConsumerChannelDisposedMessage()
    {
        var are = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((ConsumerChannelDisposedEvent _) => Task.FromResult(are.Set()));
#pragma warning restore IDISP004
        return are;
    }

    [Fact]
    public async Task Should_cleanup_publish_model()
    {
        bus.PubSub.Publish(new TestMessage());
        await mockBuilder.DisposeAsync();
        
        mockBuilder.Channels[0].Received().Dispose();
    }

    [Fact]
    public async Task Should_cleanup_request_response_model()
    {
        using var waiter = new CountdownEvent(2);

#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((PublishedMessageEvent _) => Task.FromResult(waiter.Signal()));
        mockBuilder.EventBus.Subscribe((StartConsumingSucceededEvent _) =>Task.FromResult(waiter.Signal()));
#pragma warning restore IDISP004

        _ = bus.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
        if (!waiter.Wait(5000))
            throw new TimeoutException();

        using var are = WaitForConsumerChannelDisposedMessage();

        await mockBuilder.DisposeAsync();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public async Task Should_cleanup_respond_model()
    {
        using var waiter = new CountdownEvent(1);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((StartConsumingSucceededEvent _) => Task.FromResult(waiter.Signal()));

        await bus.Rpc.RespondAsync<TestRequestMessage, TestResponseMessage>(_ => (TestResponseMessage)null);
#pragma warning restore IDISP004
        if (!waiter.Wait(5000))
            throw new TimeoutException();

        using var are = WaitForConsumerChannelDisposedMessage();

        await mockBuilder.DisposeAsync();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public async Task Should_cleanup_subscribe_async_model()
    {
#pragma warning disable IDISP004
        await bus.PubSub.SubscribeAsync<TestMessage>("abc", _ => { });
#pragma warning restore IDISP004
        using var are = WaitForConsumerChannelDisposedMessage();

        await mockBuilder.DisposeAsync();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public async Task Should_cleanup_subscribe_model()
    {
#pragma warning disable IDISP004
        await bus.PubSub.SubscribeAsync<TestMessage>("abc", _ => { });
#pragma warning restore IDISP004
        using var are = WaitForConsumerChannelDisposedMessage();

        await mockBuilder.DisposeAsync();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }
}
