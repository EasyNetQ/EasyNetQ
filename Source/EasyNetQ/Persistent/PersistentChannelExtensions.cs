using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

internal static class PersistentChannelExtensions
{
    public static ValueTask<TResult> InvokeChannelActionAsync<TResult>(
        this IPersistentChannel source, Func<IChannel, Task<TResult>> channelAction, CancellationToken cancellationToken = default
    )
    {
        return source.InvokeChannelActionAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), cancellationToken
        );
    }
}
