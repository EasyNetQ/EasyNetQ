using System.Diagnostics;
using EasyNetQ.Pipeline;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.AspNetCore.SignalR.Tests;

/// <summary>A broker outage holds a publish in the channel retry loop; the backplane bounds that wait</summary>
public sealed class When_the_broker_is_unreachable_during_a_send : IAsyncLifetime
{
    private readonly OutageTransport transport = new(new InMemoryTransport());
    private TestServer server = null!;

    public async ValueTask InitializeAsync()
    {
        server = await TestServer.StartAsync(
            "server-outage",
            services =>
            {
                services.AddSingleton<ITransport>(transport);
                services.AddEasyNetQCore();
            },
            sendTimeout: TimeSpan.FromMilliseconds(300)
        );
        await server.Hub.Clients.All.SendAsync("Message", "warm up", TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        transport.Outage = false;
        await server.DisposeAsync();
    }

    [Fact]
    public async Task Should_throw_a_timeout_within_the_send_timeout()
    {
        transport.Outage = true;
        var stopwatch = Stopwatch.StartNew();

        var send = () => server.Hub.Clients.All.SendAsync("Message", "lost", TestContext.Current.CancellationToken);

        await send.Should().ThrowAsync<TimeoutException>().WithMessage("*did not complete within*");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Should_honour_the_callers_cancellation_as_cancellation()
    {
        transport.Outage = true;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        var send = () => server.Hub.Clients.All.SendAsync("Message", "lost", cts.Token);

        await send.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_send_normally_when_the_broker_is_back()
    {
        await using var client = await server.ConnectAsync();
        transport.Outage = true;
        var lost = () => server.Hub.Clients.All.SendAsync("Message", "lost", TestContext.Current.CancellationToken);
        await lost.Should().ThrowAsync<TimeoutException>();

        transport.Outage = false;
        await server.Hub.Clients.All.SendAsync("Message", "back", TestContext.Current.CancellationToken);

        (await client.NextAsync()).Should().Be("back");
    }

    private sealed class OutageTransport(ITransport inner) : ITransport
    {
        public volatile bool Outage;

        public async ValueTask<ITransportConnection> ConnectAsync(ConnectionContext context, CancellationToken cancellationToken = default)
            => new Connection(this, await inner.ConnectAsync(context, cancellationToken));

        private sealed class Connection(OutageTransport owner, ITransportConnection inner) : ITransportConnection
        {
            public bool IsConnected => inner.IsConnected;
            public Task EnsureConnectedAsync(CancellationToken cancellationToken = default) => inner.EnsureConnectedAsync(cancellationToken);

            public async ValueTask<ITransportChannel> OpenChannelAsync(ChannelContext context, CancellationToken cancellationToken = default)
                => new Channel(owner, await inner.OpenChannelAsync(context, cancellationToken));

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }

        private sealed class Channel(OutageTransport owner, ITransportChannel inner) : ITransportChannel
        {
            public ITopology? Topology => inner.Topology;

            // what the RabbitMQ persistent channel does while the broker is down: retry until the token ends it
            public async ValueTask PublishAsync(PublishContext context)
            {
                if (owner.Outage)
                    await Task.Delay(Timeout.Infinite, context.CancellationToken);
                await inner.PublishAsync(context);
            }

            public ValueTask<ITransportConsumer> StartConsumerAsync(IReadOnlyCollection<ConsumerContext> consumers, CancellationToken cancellationToken = default)
                => inner.StartConsumerAsync(consumers, cancellationToken);

            public ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }
}
