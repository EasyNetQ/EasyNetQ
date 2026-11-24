using BenchmarkDotNet.Attributes;
using EasyNetQ.Transport;
using EasyNetQ.Transport.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Benchmarks;

/// <summary>
///     Full request/response round trip over the InMemory transport: request publish pipeline with
///     serialization, reply consumer, correlation resolution, response publish, response deserialize.
/// </summary>
[MemoryDiagnoser]
public class RpcBenchmarks
{
    public sealed record Ping(int Value);
    public sealed record Pong(int Value);

    private ServiceProvider provider = null!;
    private IRpc rpc = null!;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITransport>(new InMemoryTransport());
        services.AddEasyNetQCore();
        provider = services.BuildServiceProvider();
        rpc = provider.GetRequiredService<IRpc>();
        await rpc.RespondAsync<Ping, Pong>(
            (ping, _) => Task.FromResult(new Pong(ping.Value + 1)), _ => { }
        );
        // warm the reply subscription and the request pipeline
        await rpc.RequestAsync<Ping, Pong>(new Ping(0), _ => { });
    }

    [GlobalCleanup]
    public async Task GlobalCleanup() => await provider.DisposeAsync();

    [Benchmark]
    public Task<Pong> RequestResponse() => rpc.RequestAsync<Ping, Pong>(new Ping(41), static _ => { });
}
