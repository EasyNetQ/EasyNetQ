using System;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;

namespace EasyNetQ.Producer;

/// <summary>
/// A listener of publish confirmations
/// </summary>
public interface IPublishConfirmationListener : IDisposable
{
    /// <summary>
    /// Creates pending confirmation for a next publish
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Pending confirmation to wait</returns>
    Task<IPublishPendingConfirmation> CreatePendingConfirmation(IChannel channel,
        CancellationToken cancellationToken = default);
}
