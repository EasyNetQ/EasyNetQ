using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

public static class EasyNetQBuilderExtensions
{
    public static IEasyNetQBuilder UseMultiChannelClientCommandDispatcher(
        this IEasyNetQBuilder builder, int channelsCount
    )
    {
        builder.Services.AddSingleton<IPersistentChannelDispatcher>(
            x => new MultiPersistentChannelDispatcher(
                channelsCount,
                x.GetRequiredService<IProducerConnection>(),
                x.GetRequiredService<IConsumerConnection>(),
                x.GetRequiredService<IPersistentChannelFactory>()
            )
        );
        return builder;
    }

    public static IEasyNetQBuilder UseLegacyTypeNaming(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<ITypeNameSerializer, LegacyTypeNameSerializer>();
        return builder;
    }

    public static IEasyNetQBuilder UseLegacyRpcConventions(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConventions, LegacyRpcConventions>();
        return builder;
    }

    public static IEasyNetQBuilder UseLegacyConventions(this IEasyNetQBuilder builder)
    {
        return builder
            .UseLegacyTypeNaming()
            .UseLegacyRpcConventions();
    }

    public static IEasyNetQBuilder UseDelayedExchangeScheduler(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IScheduler, DelayedExchangeScheduler>();
        return builder;
    }

    public static IEasyNetQBuilder UseAdvancedMessagePolymorphism(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IExchangeDeclareStrategy, MultipleExchangeDeclareStrategy>();
        return builder;
    }

    public static IEasyNetQBuilder UseVersionedMessage(this IEasyNetQBuilder builder)
    {
        builder.Services
            .AddSingleton<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
            .AddSingleton<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
        return builder;
    }

    public static IEasyNetQBuilder UseAlwaysAckConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        return builder;
    }

    public static IEasyNetQBuilder UseAlwaysNackWithRequeueConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithRequeue);
        return builder;
    }

    public static IEasyNetQBuilder UseAlwaysNackWithoutRequeueConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithoutRequeue);
        return builder;
    }
}
