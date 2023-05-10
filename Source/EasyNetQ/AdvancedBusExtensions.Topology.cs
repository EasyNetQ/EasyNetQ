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
    /// Declare a transient server named queue. Note, this queue will only last for duration of the
    /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
    /// consumers.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The queue</returns>
    public static Queue QueueDeclare(this IAdvancedBus bus, CancellationToken cancellationToken = default)
    {
        return bus.QueueDeclareAsync(cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="configure">Delegate to configure the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Queue QueueDeclare(
        this IAdvancedBus bus,
        string queue,
        Action<IQueueDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        return bus.QueueDeclareAsync(queue, configure, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Task<Queue> QueueDeclareAsync(
        this IAdvancedBus bus,
        string queue,
        CancellationToken cancellationToken = default
    ) => bus.QueueDeclareAsync(queue, _ => { }, cancellationToken);

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="durable">Durable queues remain active when a server restarts.</param>
    /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
    /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Task<Queue> QueueDeclareAsync(
        this IAdvancedBus bus,
        string queue,
        bool durable,
        bool exclusive,
        bool autoDelete,
        CancellationToken cancellationToken = default
    )
    {
        return bus.QueueDeclareAsync(
            queue,
            c => c.AsDurable(durable)
                .AsExclusive(exclusive)
                .AsAutoDelete(autoDelete),
            cancellationToken
        );
    }

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="durable">Durable queues remain active when a server restarts.</param>
    /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
    /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Queue QueueDeclare(
        this IAdvancedBus bus,
        string queue,
        bool durable,
        bool exclusive,
        bool autoDelete,
        CancellationToken cancellationToken = default
    )
    {
        return bus.QueueDeclareAsync(queue, durable, exclusive, autoDelete, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare a queue. If the queue already exists this method does nothing
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>
    /// The queue
    /// </returns>
    public static Queue QueueDeclare(
        this IAdvancedBus bus,
        string queue,
        CancellationToken cancellationToken = default
    )
    {
        return bus.QueueDeclareAsync(queue, cancellationToken)
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
        IDictionary<string, object>? arguments,
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
        IDictionary<string, object>? arguments,
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
    /// Bind two exchanges. Does nothing if the binding already exists.
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="source">The source exchange</param>
    /// <param name="destination">The destination exchange</param>
    /// <param name="routingKey">The routing key</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static Binding<Exchange> Bind(
        this IAdvancedBus bus,
        Exchange source,
        Exchange destination,
        string routingKey,
        CancellationToken cancellationToken = default
    )
    {
        return bus.BindAsync(source, destination, routingKey, cancellationToken)
            .GetAwaiter()
            .GetResult();
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
    public static Binding<Exchange> Bind(
        this IAdvancedBus bus,
        Exchange source,
        Exchange destination,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    )
    {
        return bus.BindAsync(source, destination, routingKey, arguments, cancellationToken)
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
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A binding</returns>
    public static Binding<Queue> Bind(this IAdvancedBus bus, Exchange exchange, Queue queue, string routingKey, CancellationToken cancellationToken = default)
    {
        return bus.BindAsync(exchange, queue, routingKey, cancellationToken)
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
    public static Binding<Queue> Bind(
        this IAdvancedBus bus,
        Exchange exchange,
        Queue queue,
        string routingKey,
        IDictionary<string, object>? arguments,
        CancellationToken cancellationToken = default
    )
    {
        return bus.BindAsync(exchange, queue, routingKey, arguments, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange name</param>
    /// <param name="type">The type of exchange</param>
    /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
    /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    public static Task<Exchange> ExchangeDeclareAsync(
        this IAdvancedBus bus,
        string exchange,
        string type,
        bool durable = true,
        bool autoDelete = false,
        CancellationToken cancellationToken = default
    ) => bus.ExchangeDeclareAsync(exchange, c => c.AsDurable(durable).AsAutoDelete(autoDelete).WithType(type), cancellationToken);

    /// <summary>
    /// Declare a exchange passively. Throw an exception rather than create the exchange if it doesn't exist
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to declare</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void ExchangeDeclarePassive(
        this IAdvancedBus bus,
        string exchange,
        CancellationToken cancellationToken = default
    )
    {
        bus.ExchangeDeclarePassiveAsync(exchange, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange name</param>
    /// <param name="type">The type of exchange</param>
    /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
    /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    public static Exchange ExchangeDeclare(
        this IAdvancedBus bus,
        string exchange,
        string type,
        bool durable = true,
        bool autoDelete = false,
        CancellationToken cancellationToken = default
    )
    {
        return bus.ExchangeDeclareAsync(exchange, type, durable, autoDelete, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Declare an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange name</param>
    /// <param name="configure">The configuration of exchange</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    public static Exchange ExchangeDeclare(
        this IAdvancedBus bus,
        string exchange,
        Action<IExchangeDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        return bus.ExchangeDeclareAsync(exchange, configure, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

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
    /// Unbind a queue from an exchange.
    /// </summary>
    public static void QueueUnbindAsync(
        this IAdvancedBus bus,
        string queue,
        string exchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default
    )
    {
        bus.QueueUnbindAsync(queue, exchange, routingKey, arguments, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Unbind an exchange from an exchange.
    /// </summary>
    public static void ExchangeUnbindAsync(
        this IAdvancedBus bus,
        string destinationExchange,
        string sourceExchange,
        string routingKey,
        IDictionary<string, object> arguments,
        CancellationToken cancellationToken = default
    )
    {
        bus.ExchangeUnbindAsync(destinationExchange, sourceExchange, routingKey, arguments, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Delete a binding
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="binding">the binding to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Unbind(this IAdvancedBus bus, Binding<Queue> binding, CancellationToken cancellationToken = default)
    {
        bus.UnbindAsync(binding, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Delete a binding
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="binding">the binding to delete</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void Unbind(this IAdvancedBus bus, Binding<Exchange> binding, CancellationToken cancellationToken = default)
    {
        bus.UnbindAsync(binding, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Delete a queue
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="queue">The name of the queue to delete</param>
    /// <param name="ifUnused">Only delete if unused</param>
    /// <param name="ifEmpty">Only delete if empty</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void QueueDelete(
        this IAdvancedBus bus,
        string queue,
        bool ifUnused = false,
        bool ifEmpty = false,
        CancellationToken cancellationToken = default
    )
    {
        bus.QueueDeleteAsync(queue, ifUnused, ifEmpty, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Purges a queue
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="name">The name of the queue to purge</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void QueuePurge(this IAdvancedBus bus, string name, CancellationToken cancellationToken = default)
    {
        bus.QueuePurgeAsync(name, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    /// <summary>
    /// Delete an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange to delete</param>
    /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
    /// <param name="cancellationToken">The cancellation token</param>
    public static void ExchangeDelete(this IAdvancedBus bus, Exchange exchange, bool ifUnused = false, CancellationToken cancellationToken = default)
    {
        bus.ExchangeDeleteAsync(exchange, ifUnused, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

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
