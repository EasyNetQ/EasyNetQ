using EasyNetQ.Pipeline;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

/// <summary>Implemented by each hub's backplane; the lifecycle step asks it to redeclare after a recovery</summary>
internal interface IBackplaneResync
{
    Task ResyncAsync();
}

internal static class BackplaneKeys
{
    /// <summary>Set on the backplane's consumer connection context, so a recovery of that connection finds its owner</summary>
    public static readonly PropertyKey<IBackplaneResync> Owner = new("EasyNetQ.AspNetCore.SignalR.Owner");
}

/// <summary>
///     Lifecycle step: when a backplane's consumer connection recovers, or the broker cancels its consumer (queue
///     deleted, queue node lost), the server queue or its bindings may be gone (RabbitMQ.Client runs with topology
///     recovery off), so the backplane redeclares them and restarts its consumer.
/// </summary>
internal static class BackplaneRecovery
{
    public static async ValueTask OnLifecycleAsync(LifecycleContext context, PipelineStep<LifecycleContext> next)
    {
        await next(context).ConfigureAwait(false);
        var lost = (context.Layer == LifecycleLayer.Connection && context.Event == LifecycleEvent.Recovered)
            || (context.Layer == LifecycleLayer.Consumer && context.Event == LifecycleEvent.Cancelled);
        if (lost && context.TryGet(BackplaneKeys.Owner, out var owner))
            // off the recovery callback: redeclaring talks to the broker
            _ = Task.Run(owner.ResyncAsync);
    }
}
