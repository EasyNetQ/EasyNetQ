using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.MultipleExchange;

/// <inheritdoc />
public class MultipleExchangeDeclareStrategy : IExchangeDeclareStrategy, IDisposable
{
    private readonly IAdvancedBus advancedBus;
    private readonly IConventions conventions;
    private readonly AsyncCache<ExchangeKey, Exchange> declaredExchanges;
    private readonly AsyncCache<MessageTypeExchangeKey, Exchange> declaredMessageTypeExchanges;
    private bool disposed;

    public MultipleExchangeDeclareStrategy(IConventions conventions, IAdvancedBus advancedBus)
    {
        this.conventions = conventions;
        this.advancedBus = advancedBus;

        declaredExchanges = new AsyncCache<ExchangeKey, Exchange>((k, c) => advancedBus.ExchangeDeclareAsync(k.Name, k.Type, cancellationToken: c));
        // The whole declare-source + declare-and-bind-per-interface unit runs once per message type; 8.x re-ran
        // the GetInterfaces scan and the bind loop on every publish
        declaredMessageTypeExchanges = new AsyncCache<MessageTypeExchangeKey, Exchange>((k, c) => DeclareAndBindAsync(k.MessageType, k.ExchangeType, c));
    }

    /// <inheritdoc />
    public Task<Exchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
        => declaredMessageTypeExchanges.GetOrAddAsync(new MessageTypeExchangeKey(messageType, exchangeType), cancellationToken);

    private async Task<Exchange> DeclareAndBindAsync(Type messageType, string exchangeType, CancellationToken cancellationToken)
    {
        var sourceExchangeName = conventions.ExchangeNamingConvention(messageType);
        var sourceExchange = await DeclareExchangeAsync(sourceExchangeName, exchangeType, cancellationToken).ConfigureAwait(false);

        foreach (var @interface in messageType.GetInterfaces())
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

    private readonly record struct MessageTypeExchangeKey(Type MessageType, string ExchangeType);

    public virtual void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        declaredExchanges.Dispose();
        declaredMessageTypeExchanges.Dispose();
    }

}
