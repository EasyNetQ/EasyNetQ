using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
/// Bus (in fact wrapper around IBus) with an internal DI container so the caller should dispose it.
/// </summary>
public sealed class SelfHostedBus : IBus, IDisposable
{
    private readonly IBus bus;
    private readonly IServiceProvider serviceProvider;

    internal SelfHostedBus(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    /// <inheritdoc />
    public void Dispose() => (serviceProvider as IDisposable)?.Dispose();

    /// <inheritdoc />
    public IPubSub PubSub => bus.PubSub;

    /// <inheritdoc />
    public IRpc Rpc => bus.Rpc;

    /// <inheritdoc />
    public ISendReceive SendReceive => bus.SendReceive;

    /// <inheritdoc />
    public IScheduler Scheduler => bus.Scheduler;

    /// <inheritdoc />
    public IAdvancedBus Advanced => bus.Advanced;
}
