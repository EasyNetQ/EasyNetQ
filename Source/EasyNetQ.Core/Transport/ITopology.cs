namespace EasyNetQ.Transport;

/// <summary>
///     Topology operations of a transport. Definitions carry transport-agnostic flags plus an argument
///     dictionary for transport-specific settings.
/// </summary>
public interface ITopology
{
    /// <summary>Declares an exchange; idempotent when it already exists with the same settings</summary>
    ValueTask DeclareExchangeAsync(ExchangeDefinition exchange, CancellationToken cancellationToken = default);

    /// <summary>Verifies an exchange exists; throws when it does not</summary>
    ValueTask DeclareExchangePassiveAsync(string exchange, CancellationToken cancellationToken = default);

    /// <summary>Deletes an exchange</summary>
    ValueTask DeleteExchangeAsync(string exchange, bool ifUnused = false, CancellationToken cancellationToken = default);

    /// <summary>Declares a queue; returns the actual queue name (server-generated when the name is empty)</summary>
    ValueTask<string> DeclareQueueAsync(QueueDefinition queue, CancellationToken cancellationToken = default);

    /// <summary>Verifies a queue exists; throws when it does not</summary>
    ValueTask DeclareQueuePassiveAsync(string queue, CancellationToken cancellationToken = default);

    /// <summary>Deletes a queue</summary>
    ValueTask DeleteQueueAsync(string queue, bool ifUnused = false, bool ifEmpty = false, CancellationToken cancellationToken = default);

    /// <summary>Removes all messages from a queue</summary>
    ValueTask PurgeQueueAsync(string queue, CancellationToken cancellationToken = default);

    /// <summary>Binds a queue or exchange to an exchange</summary>
    ValueTask BindAsync(BindingDefinition binding, CancellationToken cancellationToken = default);

    /// <summary>Removes a binding</summary>
    ValueTask UnbindAsync(BindingDefinition binding, CancellationToken cancellationToken = default);

    /// <summary>Message and consumer counts of a queue</summary>
    ValueTask<QueueStats> GetQueueStatsAsync(string queue, CancellationToken cancellationToken = default);
}
