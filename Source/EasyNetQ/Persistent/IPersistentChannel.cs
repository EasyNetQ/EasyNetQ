using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Persistent;

public interface IPersistentChannelAction<out TResult>
{
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
    Task<TResult> InvokeChannelActionAsync<TResult, TChannelAction>(
        TChannelAction channelAction, CancellationToken cancellationToken = default
    ) where TChannelAction : struct, IPersistentChannelAction<TResult>;
}
