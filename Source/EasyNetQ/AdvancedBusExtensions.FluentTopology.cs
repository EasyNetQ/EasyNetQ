using EasyNetQ.Topology;

namespace EasyNetQ;

/// <summary>
///     Various extensions for <see cref="IAdvancedBus"/>
/// </summary>
public static partial class AdvancedBusExtensions
{
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
    public static Task<Queue> QueueDeclareAsync(
        this IAdvancedBus bus,
        string queue,
        Action<IQueueDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        var queueDeclareConfiguration = new QueueDeclareConfiguration();
        configure(queueDeclareConfiguration);

        return bus.QueueDeclareAsync(
            queue: queue,
            durable: queueDeclareConfiguration.IsDurable,
            exclusive: queueDeclareConfiguration.IsExclusive,
            autoDelete: queueDeclareConfiguration.IsAutoDelete,
            arguments: queueDeclareConfiguration.Arguments,
            cancellationToken: cancellationToken
        );
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
    /// Declare an exchange
    /// </summary>
    /// <param name="bus">The bus instance</param>
    /// <param name="exchange">The exchange name</param>
    /// <param name="configure">The configuration of exchange</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The exchange</returns>
    public static Task<Exchange> ExchangeDeclareAsync(
        this IAdvancedBus bus,
        string exchange,
        Action<IExchangeDeclareConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        var exchangeDeclareConfiguration = new ExchangeDeclareConfiguration();
        configure(exchangeDeclareConfiguration);

        return bus.ExchangeDeclareAsync(
            exchange: exchange,
            type: exchangeDeclareConfiguration.Type,
            durable: exchangeDeclareConfiguration.IsDurable,
            autoDelete: exchangeDeclareConfiguration.IsAutoDelete,
            arguments: exchangeDeclareConfiguration.Arguments,
            cancellationToken: cancellationToken
        );
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
}
