using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <inheritdoc />
public class DefaultExchangeDeclareStrategy : IExchangeDeclareStrategy
{
    private readonly IConventions conventions;
    private readonly AsyncCache<ExchangeKey, Exchange> declaredExchanges;

    public DefaultExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
    {
        this.conventions = conventions;
        declaredExchanges = new AsyncCache<ExchangeKey, Exchange>((k, t, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
    }

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(
        string exchangeName,
        string exchangeType,
        TimeBudget timeout,
        CancellationToken cancellationToken
    ) => declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), timeout, cancellationToken);

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(
        Type messageType,
        string exchangeType,
        TimeBudget timeout,
        CancellationToken cancellationToken
    )
    {
        var exchangeName = conventions.ExchangeNamingConvention(messageType);
        return DeclareExchangeAsync(exchangeName, exchangeType, timeout, cancellationToken);
    }

    private readonly record struct ExchangeKey(string Name, string Type);
}
