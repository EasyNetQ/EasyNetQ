namespace EasyNetQ;

/// <summary>
///     Publishes messages through their fluent-registered publish routes
///     (<c>bus.Publish(p =&gt; p.Exchange("orders").Message&lt;OrderPlaced&gt;("order.placed"))</c>).
///     The route decides the exchange and routing key; a message type without a route is an error.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    ///     Publishes <paramref name="message" /> through the route registered for <typeparamref name="T" />
    /// </summary>
    ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes <paramref name="message" /> through the route registered for <typeparamref name="T" />,
    ///     overriding the routing key
    /// </summary>
    ValueTask PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default);
}
