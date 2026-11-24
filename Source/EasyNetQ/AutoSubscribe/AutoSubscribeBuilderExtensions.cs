using EasyNetQ.AutoSubscribe;
using EasyNetQ.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ;

/// <summary>
/// Settings of <see cref="AutoSubscribeBuilderExtensions.AutoSubscribe"/>
/// </summary>
public sealed class AutoSubscribeOptions
{
    internal AutoSubscribeOptions(string subscriptionIdPrefix) => SubscriptionIdPrefix = subscriptionIdPrefix;

    /// <summary>The <see cref="AutoSubscriber.SubscriptionIdPrefix"/></summary>
    public string SubscriptionIdPrefix { get; }

    /// <summary>Customizes the <see cref="AutoSubscriber"/> (subscription ids, subscription configuration, dispatcher)</summary>
    public Action<AutoSubscriber>? Configure { get; set; }

    /// <summary>Selects which generated consumers to subscribe; all of them when unset</summary>
    public Func<AutoSubscriberConsumerInfo, bool>? Filter { get; set; }
}

/// <summary>
/// The 8.x AutoSubscriber on generated registrations: trim- and Native-AOT-safe
/// </summary>
public static class AutoSubscribeBuilderExtensions
{
    /// <summary>
    /// Subscribes every <see cref="IConsume{T}"/>/<see cref="IConsumeAsync{T}"/> implementation the EasyNetQ source
    /// generator found (this assembly and referenced ones) through <c>IPubSub.SubscribeAsync</c>, with the 8.x
    /// AutoSubscriber rules for subscription ids, <see cref="ForTopicAttribute"/> and
    /// <see cref="SubscriptionConfigurationAttribute"/>. Consumers are resolved from DI in a scope per message and are
    /// registered as transient unless registered already. Subscriptions start in the background and retry like the
    /// fluent consumers (<c>ConsumerHost(...)</c>); <c>IConsumerHostStatus</c> covers both.
    /// </summary>
    /// <param name="builder">The builder; call this after <c>AddEasyNetQ(...)</c>, which registers the generated consumers</param>
    /// <param name="subscriptionIdPrefix">The <see cref="AutoSubscriber.SubscriptionIdPrefix"/></param>
    /// <param name="configure">Customizes the subscription</param>
    public static IEasyNetQBuilder AutoSubscribe(this IEasyNetQBuilder builder, string subscriptionIdPrefix, Action<AutoSubscribeOptions>? configure = null)
    {
        var options = new AutoSubscribeOptions(subscriptionIdPrefix);
        configure?.Invoke(options);

        var sources = builder.Services
            .Where(d => d.ServiceType == typeof(IAutoSubscriberConsumerSource))
            .Select(d => d.ImplementationInstance)
            .OfType<IAutoSubscriberConsumerSource>()
            .ToList();
        if (sources.Count == 0)
            throw new EasyNetQException(
                "AutoSubscribe found no generated consumers: implement IConsume<T> or IConsumeAsync<T> in a project that references "
                + "EasyNetQ (the source generator emits them) and call AutoSubscribe after AddEasyNetQ(...)");
        foreach (var consumer in sources.SelectMany(s => s.Consumers))
        {
            if (options.Filter?.Invoke(consumer.Info) ?? true)
                consumer.Register(builder.Services);
        }

        builder.Services.AddSingleton(options);
        EasyNetQBuilderConsumeExtensions.AddConsumerHostStatus(builder.Services);
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, AutoSubscriberHostedService>());
        return builder;
    }
}
