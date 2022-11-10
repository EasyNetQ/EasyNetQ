using System;

namespace EasyNetQ;

public sealed class SelfHostedBus : IBus, IDisposable
{
    private readonly IBus bus;
    private readonly Action disposer;

    internal SelfHostedBus(IBus bus, Action disposer)
    {
        this.bus = bus;
        this.disposer = disposer;
    }

    public void Dispose() => disposer();

    public IPubSub PubSub => bus.PubSub;
    public IRpc Rpc => bus.Rpc;
    public ISendReceive SendReceive => bus.SendReceive;
    public IScheduler Scheduler => bus.Scheduler;
    public IAdvancedBus Advanced => bus.Advanced;
}
