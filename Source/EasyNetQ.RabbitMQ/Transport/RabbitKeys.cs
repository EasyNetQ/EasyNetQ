using EasyNetQ.Pipeline;
using EasyNetQ.Topology;

namespace EasyNetQ.Transport;

/// <summary>
///     Property keys the RabbitMQ transport reads from layer contexts
/// </summary>
public static class RabbitKeys
{
    /// <summary>
    ///     The full queue record for a consumer context; exclusivity and durability drive restart semantics
    /// </summary>
    public static readonly PropertyKey<Queue> Queue = new("EasyNetQ.RabbitMQ.Queue");

    /// <summary>
    ///     Consumer tag for a consumer context
    /// </summary>
    public static readonly PropertyKey<string> ConsumerTag = new("EasyNetQ.RabbitMQ.ConsumerTag");

    /// <summary>
    ///     Exclusive-consumer flag for a consumer context
    /// </summary>
    public static readonly PropertyKey<bool> ExclusiveConsumer = new("EasyNetQ.RabbitMQ.ExclusiveConsumer");

    /// <summary>
    ///     basic.consume arguments for a consumer context
    /// </summary>
    public static readonly PropertyKey<IDictionary<string, object>> ConsumerArguments = new("EasyNetQ.RabbitMQ.ConsumerArguments");
}
