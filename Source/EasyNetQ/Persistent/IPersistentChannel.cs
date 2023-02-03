using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

/// <summary>
/// An abstract action to run on top of the channel
/// </summary>
public interface IPersistentChannelAction<out TResult>
{
    /// <summary>
    /// Runs an abstract action
    /// </summary>
    TResult Invoke(IModel model);
}

/// <summary>
/// An abstraction on top of channel which manages its persistence and invokes an action on it
/// </summary>
public interface IPersistentChannel : IDisposable
{
    /// <summary>
    /// Invoke an action on channel
    /// </summary>
    /// <param name="channelAction">The action to invoke</param>
    /// <param name="cancellationToken">The cancellation token</param>
    ValueTask<TResult> InvokeChannelActionAsync<TResult, TChannelAction>(
        TChannelAction channelAction, CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>;
}
