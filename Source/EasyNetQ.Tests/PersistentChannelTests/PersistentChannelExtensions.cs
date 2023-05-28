using EasyNetQ.Internals;
using RabbitMQ.Client;

// ReSharper disable once CheckNamespace
namespace EasyNetQ.Persistent;

internal static class PersistentChannelExtensions
{
    public static void InvokeChannelAction(
        this IPersistentChannel source,
        Action<IModel> channelAction,
        TimeoutToken timeoutToken = default,
        CancellationToken cancellationToken = default
    )
    {
        source.InvokeChannelActionAsync(channelAction, timeoutToken, cancellationToken)
            .AsTask()
            .GetAwaiter()
            .GetResult();
    }

    public static ValueTask<bool> InvokeChannelActionAsync(
        this IPersistentChannel source,
        Action<IModel> channelAction,
        TimeoutToken timeoutToken = default,
        CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), timeoutToken, cancellationToken
        );
    }

    public static ValueTask<TResult> InvokeChannelActionAsync<TResult>(
        this IPersistentChannel source,
        Func<IModel, TResult> channelAction,
        TimeoutToken timeoutToken = default,
        CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), timeoutToken, cancellationToken
        );
    }
}
