using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;

namespace EasyNetQ.Tests;

public class ModelCleanupTests : IDisposable
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

    public virtual void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        mockBuilder.Dispose();
    }

    private AutoResetEvent WaitForConsumerChannelDisposedMessage()
    {
        var are = new AutoResetEvent(false);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((in ConsumerChannelDisposedEvent _) => are.Set());
#pragma warning restore IDISP004
        return are;
    }

    [Fact]
    public void Should_cleanup_publish_model()
    {
        bus.PubSub.Publish(new TestMessage());
        mockBuilder.Dispose();

        mockBuilder.Channels[0].Received().Dispose();
    }

    [Fact]
    public void Should_cleanup_request_response_model()
    {
        using var waiter = new CountdownEvent(2);

#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((in PublishedMessageEvent _) => waiter.Signal());
        mockBuilder.EventBus.Subscribe((in StartConsumingSucceededEvent _) => waiter.Signal());
#pragma warning restore IDISP004

        bus.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(new TestRequestMessage());
        if (!waiter.Wait(5000))
            throw new TimeoutException();

        using var are = WaitForConsumerChannelDisposedMessage();

        mockBuilder.Dispose();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public void Should_cleanup_respond_model()
    {
        using var waiter = new CountdownEvent(1);
#pragma warning disable IDISP004
        mockBuilder.EventBus.Subscribe((in StartConsumingSucceededEvent _) => waiter.Signal());

        bus.Rpc.Respond<TestRequestMessage, TestResponseMessage>(_ => (TestResponseMessage)null);
#pragma warning restore IDISP004
        if (!waiter.Wait(5000))
            throw new TimeoutException();

        using var are = WaitForConsumerChannelDisposedMessage();

        mockBuilder.Dispose();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public void Should_cleanup_subscribe_async_model()
    {
#pragma warning disable IDISP004
        bus.PubSub.Subscribe<TestMessage>("abc", _ => { });
#pragma warning restore IDISP004
        using var are = WaitForConsumerChannelDisposedMessage();

        mockBuilder.Dispose();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }

    [Fact]
    public void Should_cleanup_subscribe_model()
    {
#pragma warning disable IDISP004
        bus.PubSub.Subscribe<TestMessage>("abc", _ => { });
#pragma warning restore IDISP004
        using var are = WaitForConsumerChannelDisposedMessage();

        mockBuilder.Dispose();

        var signalReceived = are.WaitOne(waitTime);
        Assert.True(signalReceived, $"Set event was not received within {waitTime.TotalSeconds} seconds");

        mockBuilder.Channels[0].Received().Dispose();
        mockBuilder.Channels[1].Received().Dispose();
    }
}
