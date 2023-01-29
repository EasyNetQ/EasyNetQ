using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.MultipleExchange;

/// <inheritdoc />
public class MultipleExchangeDeclareStrategy : IExchangeDeclareStrategy
{
    private readonly IAdvancedBus advancedBus;
    private readonly IConventions conventions;
    private readonly AsyncCache<ExchangeKey, Exchange> declaredExchanges;

    public MultipleExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
    {
        this.conventions = conventions;
        this.advancedBus = advancedBus;

        declaredExchanges = new AsyncCache<ExchangeKey, Exchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
    }

    /// <inheritdoc />
    public async Task<Exchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
    {
        var sourceExchangeName = conventions.ExchangeNamingConvention(messageType);
        var sourceExchange = await DeclareExchangeAsync(sourceExchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
        var interfaces = messageType.GetInterfaces();

        foreach (var @interface in interfaces)
        {
            var destinationExchangeName = conventions.ExchangeNamingConvention(@interface);
            var destinationExchange = await DeclareExchangeAsync(destinationExchangeName, exchangeType, cancellationToken).ConfigureAwait(false);
            await advancedBus.BindAsync(sourceExchange, destinationExchange, "#", cancellationToken).ConfigureAwait(false);
        }

        return sourceExchange;
    }

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
    {
        return declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), cancellationToken);
    }

    private readonly record struct ExchangeKey(string Name, string Type);
}
