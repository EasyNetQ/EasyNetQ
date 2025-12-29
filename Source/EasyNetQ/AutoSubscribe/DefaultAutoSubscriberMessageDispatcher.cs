using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.AutoSubscribe;

public class DefaultAutoSubscriberMessageDispatcher(IServiceProvider resolver) : IAutoSubscriberMessageDispatcher
{
    /// <inheritdoc />
    public void Dispatch<TMessage, TConsumer>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        where TConsumer : class, IConsume<TMessage>
    {
        using var scope = resolver.CreateScope();
        var consumer = scope.ServiceProvider.GetRequiredService<TConsumer>();
        consumer.Consume(message, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DispatchAsync<TMessage, TAsyncConsumer>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : class
        where TAsyncConsumer : class, IConsumeAsync<TMessage>
    {
        using var scope = resolver.CreateScope();
        var asyncConsumer = scope.ServiceProvider.GetRequiredService<TAsyncConsumer>();
        await asyncConsumer.ConsumeAsync(message, cancellationToken).ConfigureAwait(false);
    }
}
