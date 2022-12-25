using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using RabbitMQ.Client;

namespace EasyNetQ.ChannelDispatcher;

internal static class PersistentChannelDispatcherExtensions
{
    public static Task InvokeAsync(
        this IPersistentChannelDispatcher dispatcher,
        Action<IModel> channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<NoResult, ActionBasedPersistentChannelAction>(
            new ActionBasedPersistentChannelAction(channelAction), options, cancellationToken
        );
    }

    public static Task<TResult> InvokeAsync<TResult>(
        this IPersistentChannelDispatcher dispatcher,
        Func<IModel, TResult> channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    )
    {
        return dispatcher.InvokeAsync<TResult, FuncBasedPersistentChannelAction<TResult>>(
            new FuncBasedPersistentChannelAction<TResult>(channelAction), options, cancellationToken
        );
    }
}
