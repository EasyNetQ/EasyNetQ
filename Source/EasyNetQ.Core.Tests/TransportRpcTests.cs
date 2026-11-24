using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Core.Tests;

public class TransportRpcTests
{
    public sealed record Ping(int Value);
    public sealed record Pong(int Value);

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(new InMemoryTransport());
        services.AddEasyNetQCore();
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Should_round_trip_request_and_response()
    {
        await using var provider = CreateProvider();
        var rpc = provider.GetRequiredService<IRpc>();
        rpc.Should().BeOfType<TransportRpc>();

        await using var responder = await rpc.RespondAsync<Ping, Pong>(
            (ping, _) => Task.FromResult(new Pong(ping.Value * 2)),
            _ => { },
            TestContext.Current.CancellationToken
        );

        var pong = await rpc.RequestAsync<Ping, Pong>(
            new Ping(21), _ => { }, TestContext.Current.CancellationToken
        );

        pong.Should().Be(new Pong(42));
    }

    [Fact]
    public async Task Should_propagate_responder_failures()
    {
        await using var provider = CreateProvider();
        var rpc = provider.GetRequiredService<IRpc>();

        await using var responder = await rpc.RespondAsync<Ping, Pong>(
            (_, _) => Task.FromException<Pong>(new InvalidOperationException("responder blew up")),
            _ => { },
            TestContext.Current.CancellationToken
        );

        var request = async () => await rpc.RequestAsync<Ping, Pong>(
            new Ping(1), _ => { }, TestContext.Current.CancellationToken
        );

        await request.Should().ThrowAsync<EasyNetQResponderException>().WithMessage("responder blew up");
    }

    [Fact]
    public async Task Should_time_out_without_a_responder()
    {
        await using var provider = CreateProvider();
        var rpc = provider.GetRequiredService<IRpc>();

        var request = async () => await rpc.RequestAsync<Ping, Pong>(
            new Ping(1),
            x => x.WithExpiration(TimeSpan.FromMilliseconds(200)),
            TestContext.Current.CancellationToken
        );

        await request.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Should_serve_concurrent_requests()
    {
        await using var provider = CreateProvider();
        var rpc = provider.GetRequiredService<IRpc>();

        await using var responder = await rpc.RespondAsync<Ping, Pong>(
            async (ping, ct) =>
            {
                await Task.Delay(10, ct);
                return new Pong(ping.Value + 1);
            },
            _ => { },
            TestContext.Current.CancellationToken
        );

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 20).Select(i => rpc.RequestAsync<Ping, Pong>(
                new Ping(i), _ => { }, TestContext.Current.CancellationToken
            ))
        );

        responses.Select(r => r.Value).Should().BeEquivalentTo(Enumerable.Range(1, 20));
    }
}
