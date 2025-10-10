using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher;

internal static class PersistentChannelDispatcherExtensions
{
    public static Task<bool> InvokeAsync(
        this IPersistentChannelDispatcher dispatcher,
        Func<IChannel, Task> channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<bool, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), options, cancellationToken
        );
    }

    public static Task<TResult> InvokeAsync<TResult>(
        this IPersistentChannelDispatcher dispatcher,
        Func<IChannel, Task<TResult>> channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), options, cancellationToken
        );
    }
}
