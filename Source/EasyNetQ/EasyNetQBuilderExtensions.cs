using EasyNetQ.ChannelDispatcher;
using EasyNetQ.Consumer;
using EasyNetQ.Interception;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

    public static ProducePipelineBuilder UseProduceInterceptors(this ProducePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.Services.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>()
                .ToArray();
            var producedMessage = interceptors.OnProduce(new ProducedMessage(ctx.Properties, ctx.Body));
            return next(ctx with { Properties = producedMessage.Properties, Body = producedMessage.Body });
        });
    }

    public static ConsumePipelineBuilder UseConsumeInterceptors(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.Services.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>()
                .ToArray();
            var consumedMessage =
                interceptors.OnConsume(new ConsumedMessage(ctx.ReceivedInfo, ctx.Properties, ctx.Body));
            return next(ctx with
            {
                ReceivedInfo = consumedMessage.ReceivedInfo,
                Properties = consumedMessage.Properties,
                Body = consumedMessage.Body
            });
        });
    }

    public static ConsumePipelineBuilder UseScope(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => async ctx =>
        {
            using var scopedResolver = ctx.Services.CreateScope();
            return await next(ctx with { Services = scopedResolver.ServiceProvider }).ConfigureAwait(false);
        });
    }

    public static ConsumePipelineBuilder UseConsumeErrorStrategy(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => async ctx =>
        {
            var errorStrategy = ctx.Services.GetRequiredService<IConsumeErrorStrategy>();
            var logger = ctx.Services.GetRequiredService<ILogger<IConsumeErrorStrategy>>();

            try
            {
                try
                {
                    return await next(ctx).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ctx.CancellationToken.IsCancellationRequested)
                {
                    return await errorStrategy.HandleCancelledAsync(ctx).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    return await errorStrategy.HandleErrorAsync(ctx, exception).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Consume error strategy has failed");

                return AckStrategies.NackWithRequeueAsync;
            }
        });
    }
}
