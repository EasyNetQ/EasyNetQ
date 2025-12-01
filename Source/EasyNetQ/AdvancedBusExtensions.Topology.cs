using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
{
    /// <summary>
    /// Gets stats for the given queue
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The stats of the queue</returns>
    public static QueueStats GetQueueStats(
        this IAdvancedBus bus, string queue, CancellationToken cancellationToken = default
    )
    {
        return bus.GetQueueStatsAsync(queue, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="durable">Durable queues remain active when a server restarts.</param>
    /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
    /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
    /// <param name="arguments"></param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Queue QueueDeclare(
        this IAdvancedBus bus,
        string queue,
        bool durable = true,
        bool exclusive = false,
        bool autoDelete = false,
        IDictionary<string, object>? arguments = null,
        CancellationToken cancellationToken = default
    )
    {
        return bus.QueueDeclareAsync(queue, durable, exclusive, autoDelete, arguments, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare a queue passively. Throw an exception rather than create the queue if it doesn't exist
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The queue to declare</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void QueueDeclarePassive(
        this IAdvancedBus bus,
        string queue,
        CancellationToken cancellationToken = default
    )
    {
        bus.QueueDeclarePassiveAsync(queue, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }


    /// <summary>
    /// Bind an exchange to a queue. Does nothing if the binding already exists.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to bind</param>
    /// <param name="queue">The queue to bind</param>
    /// <param name="routingKey">The routing key</param>
    /// <param name="arguments">The arguments</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static async Task<Binding<Queue>> BindAsync(
        this IAdvancedBus bus,
        Exchange exchange,
        Queue queue,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default
    )
    {
        await bus.QueueBindAsync(queue.Name, exchange.Name, routingKey, arguments, cancellationToken).ConfigureAwait(false);
        return new Binding<Queue>(exchange, queue, routingKey, arguments);
    }

    /// <summary>
    /// Bind two exchanges. Does nothing if the binding already exists.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="source">The source exchange</param>
    /// <param name="destination">The destination exchange</param>
    /// <param name="routingKey">The routing key</param>
    /// <param name="arguments">The arguments</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static async Task<Binding<Exchange>> BindAsync(
        this IAdvancedBus bus,
        Exchange source,
        Exchange destination,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default
    )
    {
        await bus.ExchangeBindAsync(destination.Name, source.Name, routingKey, arguments, cancellationToken).ConfigureAwait(false);
        return new Binding<Exchange>(source, destination, routingKey, arguments);
    }

    /// <summary>
    /// Bind two exchanges. Does nothing if the binding already exists.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="source">The exchange</param>
    /// <param name="queue">The queue</param>
    /// <param name="routingKey">The routing key</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static Task<Binding<Queue>> BindAsync(
        this IAdvancedBus bus,
        Exchange source,
        Queue queue,
        string routingKey,
        CancellationToken cancellationToken = default
    ) => bus.BindAsync(source, queue, routingKey, null, cancellationToken);

    /// <summary>
    /// Bind two exchanges. Does nothing if the binding already exists.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="source">The source exchange</param>
    /// <param name="destination">The destination exchange</param>
    /// <param name="routingKey">The routing key</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static Task<Binding<Exchange>> BindAsync(
        this IAdvancedBus bus,
        Exchange source,
        Exchange destination,
        string routingKey,
        CancellationToken cancellationToken = default
    ) => bus.BindAsync(source, destination, routingKey, null, cancellationToken);


    /// <summary>
    /// Delete a binding
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="binding">the binding to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task UnbindAsync(this IAdvancedBus bus, Binding<Queue> binding, CancellationToken cancellationToken = default)
        => bus.QueueUnbindAsync(binding.Destination.Name, binding.Source.Name, binding.RoutingKey, binding.Arguments, cancellationToken);

    /// <summary>
    /// Delete a binding
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="binding">the binding to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task UnbindAsync(this IAdvancedBus bus, Binding<Exchange> binding, CancellationToken cancellationToken = default)
        => bus.ExchangeUnbindAsync(binding.Destination.Name, binding.Source.Name, binding.RoutingKey, binding.Arguments, cancellationToken);

    /// <summary>
    /// Delete an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to delete</param>
    /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static Task ExchangeDeleteAsync(this IAdvancedBus bus, Exchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
        => bus.ExchangeDeleteAsync(exchange.Name, ifUnused, cancellationToken);
}
