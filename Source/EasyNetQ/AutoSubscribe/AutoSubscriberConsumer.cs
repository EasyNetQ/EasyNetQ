using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EasyNetQ.AutoSubscribe;

/// <summary>
/// One consumer for <see cref="AutoSubscriber.SubscribeAsync(IEnumerable{AutoSubscriberConsumer}, CancellationToken)"/>,
/// closed over its message and consumer types, so subscribing needs no reflection. The EasyNetQ source generator
/// emits one per <see cref="IConsume{T}"/>/<see cref="IConsumeAsync{T}"/> implementation.
/// </summary>
public sealed class AutoSubscriberConsumer
{
    private readonly Func<AutoSubscriber, AutoSubscriberConsumerInfo, CancellationToken, Task<SubscriptionResult>> subscribe;
    private readonly Action<IServiceCollection> register;

    private AutoSubscriberConsumer(
        AutoSubscriberConsumerInfo info,
        Func<AutoSubscriber, AutoSubscriberConsumerInfo, CancellationToken, Task<SubscriptionResult>> subscribe,
        Action<IServiceCollection> register
    )
    {
        Info = info;
        this.subscribe = subscribe;
        this.register = register;
    }

    /// <summary>The consumer and its subscription metadata</summary>
    public AutoSubscriberConsumerInfo Info { get; }

    /// <summary>An <see cref="IConsumeAsync{T}"/> consumer</summary>
    public static AutoSubscriberConsumer Async<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConsumer>(
        AutoSubscriberConsumerAttribute? subscriptionAttribute = null,
        IReadOnlyList<string>? topics = null,
        SubscriptionConfigurationAttribute? subscriptionConfiguration = null
    )
        where TMessage : class
        where TConsumer : class, IConsumeAsync<TMessage>
        => new(
            new AutoSubscriberConsumerInfo(typeof(TConsumer), typeof(IConsumeAsync<TMessage>), typeof(TMessage), subscriptionAttribute, topics ?? [], subscriptionConfiguration),
            static (autoSubscriber, info, cancellationToken) => autoSubscriber.SubscribeAsyncConsumerAsync<TMessage, TConsumer>(info, cancellationToken),
            static services => services.TryAddTransient<TConsumer>()
        );

    /// <summary>An <see cref="IConsume{T}"/> consumer</summary>
    public static AutoSubscriberConsumer Sync<TMessage, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConsumer>(
        AutoSubscriberConsumerAttribute? subscriptionAttribute = null,
        IReadOnlyList<string>? topics = null,
        SubscriptionConfigurationAttribute? subscriptionConfiguration = null
    )
        where TMessage : class
        where TConsumer : class, IConsume<TMessage>
        => new(
            new AutoSubscriberConsumerInfo(typeof(TConsumer), typeof(IConsume<TMessage>), typeof(TMessage), subscriptionAttribute, topics ?? [], subscriptionConfiguration),
            static (autoSubscriber, info, cancellationToken) => autoSubscriber.SubscribeConsumerAsync<TMessage, TConsumer>(info, cancellationToken),
            static services => services.TryAddTransient<TConsumer>()
        );

    internal Task<SubscriptionResult> SubscribeAsync(AutoSubscriber autoSubscriber, CancellationToken cancellationToken)
        => subscribe(autoSubscriber, Info, cancellationToken);

    /// <summary>Registers the consumer class as transient unless it is registered already</summary>
    internal void Register(IServiceCollection services) => register(services);
}
