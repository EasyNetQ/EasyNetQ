using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

internal static class PersistentChannelExtensions
{
    public static void InvokeChannelAction(
        this IPersistentChannel source, Func<IChannel, Task> channelAction, CancellationToken cancellationToken = default
    )
    {
        source.InvokeChannelActionAsync(channelAction, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public static ValueTask<bool> InvokeChannelActionAsync(
        this IPersistentChannel source, Func<IChannel, Task> channelAction, CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), cancellationToken
        );
    }

    public static ValueTask<TResult> InvokeChannelActionAsync<TResult>(
        this IPersistentChannel source, Func<IChannel, Task<TResult>> channelAction, CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), cancellationToken
        );
    }
}
