using EasyNetQ.Persistent;

namespace EasyNetQ.ChannelDispatcher;

/// <summary>
///     Responsible for invoking client commands.
/// </summary>
public interface IPersistentChannelDispatcher : IAsyncDisposable
{
    /// <summary>
    /// Invokes an action on top of model
    /// </summary>
    /// <param name="channelAction"></param>
    /// <param name="options"></param>
    /// <param name="cancellationToken"></param>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TChannelAction"></typeparam>
    /// <returns></returns>
    Task<TResult> InvokeAsync<TResult, TChannelAction>(
        TChannelAction channelAction,
        PersistentChannelDispatchOptions options,
        CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>;
}
