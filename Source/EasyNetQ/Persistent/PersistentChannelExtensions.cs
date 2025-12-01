using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

internal static class PersistentChannelExtensions
{
    public static Task<bool> InvokeChannelActionAsync(
        this IPersistentChannel source, Func<IChannel, Task> channelAction, CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), cancellationToken
        );
    }

    public static Task<TResult> InvokeChannelActionAsync<TResult>(
        this IPersistentChannel source, Func<IChannel, Task<TResult>> channelAction, CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), cancellationToken
        );
    }
}
