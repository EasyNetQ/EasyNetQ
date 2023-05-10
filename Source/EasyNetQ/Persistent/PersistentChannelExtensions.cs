using EasyNetQ.Internals;
using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

internal static class PersistentChannelExtensions
{
    public static void InvokeChannelAction(
        this IPersistentChannel source,
        Action<IModel> channelAction,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    )
    {
        source.InvokeChannelActionAsync(channelAction, timeout, cancellationToken)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    public static ValueTask<bool> InvokeChannelActionAsync(
        this IPersistentChannel source,
        Action<IModel> channelAction,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction),
            timeout,
            cancellationToken
        );
    }

    public static ValueTask<TResult> InvokeChannelActionAsync<TResult>(
        this IPersistentChannel source,
        Func<IModel, TResult> channelAction,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction),
            timeout,
            cancellationToken
        );
    }
}
