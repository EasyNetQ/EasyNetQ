using System;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ;

/// <inheritdoc />
public sealed class DefaultExchangeDeclareStrategy : IExchangeDeclareStrategy, IDisposable
{
    private readonly IConventions conventions;
    private readonly AsyncCache<ExchangeKey, Exchange> declaredExchanges;
    private bool disposed;

    public DefaultExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
    {
        this.conventions = conventions;
        declaredExchanges = new AsyncCache<ExchangeKey, Exchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
    }

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken)
    {
        return declaredExchanges.GetOrAddAsync(new ExchangeKey(exchangeName, exchangeType), cancellationToken);
    }

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
    {
        var exchangeName = conventions.ExchangeNamingConvention(messageType);
        return DeclareExchangeAsync(exchangeName, exchangeType, cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        declaredExchanges.Dispose();
    }

    private readonly record struct ExchangeKey(string Name, string Type);
}
