using EasyNetQ.DI;

namespace EasyNetQ.AutoSubscribe;

public class DefaultAutoSubscriberMessageDispatcher : IAutoSubscriberMessageDispatcher
{
    private readonly IServiceResolver resolver;

    public DefaultAutoSubscriberMessageDispatcher(IServiceResolver resolver) => this.resolver = resolver;

    public DefaultAutoSubscriberMessageDispatcher() : this(new ActivatorBasedResolver())
    {
    }

    /// <inheritdoc />
    public void Dispatch<TMessage, TConsumer>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        where TConsumer : class, IConsume<TMessage>
    {
        using var scope = resolver.CreateScope();
        var consumer = scope.Resolve<TConsumer>();
        consumer.Consume(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        where TAsyncConsumer : class, IConsumeAsync<TMessage>
    {
        var scope = resolver.CreateScope();
        try
        {
            var asyncConsumer = scope.Resolve<TAsyncConsumer>();
            await asyncConsumer.ConsumeAsync(message, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (scope is IAsyncDisposable ad)
                await ad.DisposeAsync().ConfigureAwait(false);
            else
                scope.Dispose();
        }
    }

    private sealed class ActivatorBasedResolver : IServiceResolver
    {
        public TService Resolve<TService>() where TService : class => Activator.CreateInstance<TService>();

        public IServiceResolverScope CreateScope() => new ServiceResolverScope(this);
    }
}
