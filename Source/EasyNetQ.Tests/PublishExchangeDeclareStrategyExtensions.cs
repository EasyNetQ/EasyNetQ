// ReSharper disable once CheckNamespace

namespace EasyNetQ.Topology;

public static class PublishExchangeDeclareStrategyExtensions
{
    public static Exchange DeclareExchange(
        this IExchangeDeclareStrategy strategy,
        string exchangeName,
        string exchangeType,
        CancellationToken cancellationToken = default
    )
    {
        return strategy.DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }

    public static Exchange DeclareExchange(
        this IExchangeDeclareStrategy strategy,
        Type messageType,
        string exchangeType,
        CancellationToken cancellationToken = default
    )
    {
        return strategy.DeclareExchangeAsync(messageType, exchangeType, cancellationToken)
            .GetAwaiter()
            .GetResult();
    }
}
