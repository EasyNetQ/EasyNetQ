using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher;

internal static class PersistentChannelDispatcherExtensions
{
    public static ValueTask<bool> InvokeAsync(
        this IPersistentChannelDispatcher dispatcher,
        Action<IModel> channelAction,
        PersistentChannelDispatchOptions options,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), options, timeout, cancellationToken
        );
    }

    public static ValueTask<TResult> InvokeAsync<TResult>(
        this IPersistentChannelDispatcher dispatcher,
        Func<IModel, TResult> channelAction,
        PersistentChannelDispatchOptions options,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), options, timeout, cancellationToken
        );
    }
}
