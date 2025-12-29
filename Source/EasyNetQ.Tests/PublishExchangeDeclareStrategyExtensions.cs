using EasyNetQ.Topology;

namespace EasyNetQ.Tests;

public static class PublishExchangeDeclareStrategyExtensions
{
    public static Task<Exchange> DeclareExchangeAsync(
        this IExchangeDeclareStrategy strategy,
        string exchangeName,
        string exchangeType,
        CancellationToken cancellationToken = default
    )
    {
        return strategy.DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken);
    }

    public static Task<Exchange> DeclareExchangeAsync(
        this IExchangeDeclareStrategy strategy,
        Type messageType,
        string exchangeType,
        CancellationToken cancellationToken = default
    )
    {
        return strategy.DeclareExchangeAsync(messageType, exchangeType, cancellationToken);
    }
}
