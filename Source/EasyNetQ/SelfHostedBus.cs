using EasyNetQ.LightInject;

namespace EasyNetQ;

/// <summary>
/// Bus (in fact wrapper around IBus) with an internal DI container so the caller should dispose it.
/// </summary>
public sealed class SelfHostedBus : IBus, IDisposable
{
    private readonly IBus bus;
    private readonly ServiceContainer container;

    internal SelfHostedBus(ServiceContainer container)
    {
        this.bus = container.GetInstance<IBus>();
        this.container = container;
    }

    /// <inheritdoc />
    public void Dispose() => container.Dispose();

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
