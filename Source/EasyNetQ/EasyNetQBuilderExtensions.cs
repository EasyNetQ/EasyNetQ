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
    public static IEasyNetQBuilder EnableMultiChannelClientCommandDispatcher(
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

    public static IEasyNetQBuilder EnableLegacyTypeNaming(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<ITypeNameSerializer, LegacyTypeNameSerializer>();
        return builder;
    }

    public static IEasyNetQBuilder EnableLegacyRpcConventions(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConventions, LegacyRpcConventions>();
        return builder;
    }

    public static IEasyNetQBuilder EnableLegacyConventions(this IEasyNetQBuilder builder)
    {
        return builder
            .EnableLegacyTypeNaming()
            .EnableLegacyRpcConventions();
    }

    public static IEasyNetQBuilder EnableDelayedExchangeScheduler(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IScheduler, DelayedExchangeScheduler>();
        return builder;
    }

    public static IEasyNetQBuilder EnableAdvancedMessagePolymorphism(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IExchangeDeclareStrategy, MultipleExchangeDeclareStrategy>();
        return builder;
    }

    public static IEasyNetQBuilder EnableMessageVersioning(this IEasyNetQBuilder builder)
    {
        builder.Services
            .AddSingleton<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
            .AddSingleton<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
        return builder;
    }

    public static IEasyNetQBuilder EnableAlwaysAckConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);
        return builder;
    }

    public static IEasyNetQBuilder EnableAlwaysNackWithRequeueConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithRequeue);
        return builder;
    }

    public static IEasyNetQBuilder EnableAlwaysNackWithoutRequeueConsumerErrorStrategy(this IEasyNetQBuilder builder)
    {
        builder.Services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithoutRequeue);
        return builder;
    }

    public static ProducePipelineBuilder UseProduceInterceptors(this ProducePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.ServiceResolver.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>()
                .ToArray();
            var producedMessage = interceptors.OnProduce(new ProducedMessage(ctx.Properties, ctx.Body));
            return next(ctx with { Properties = producedMessage.Properties, Body = producedMessage.Body });
        });
    }

    public static ConsumePipelineBuilder UseConsumeInterceptors(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.ServiceResolver.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>()
                .ToArray();
            var consumedMessage =
                interceptors.OnConsume(new ConsumedMessage(ctx.ReceivedInfo, ctx.Properties, ctx.Body));
            return next(ctx with
            {
                ReceivedInfo = consumedMessage.ReceivedInfo, Properties = consumedMessage.Properties,
                Body = consumedMessage.Body
            });
        });
    }

    public static ConsumePipelineBuilder UseScope(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => async ctx =>
        {
            var scopedResolver = ctx.ServiceResolver.CreateScope();
            return await next(ctx with { ServiceResolver = scopedResolver.ServiceProvider }).ConfigureAwait(false);
        });
    }

    public static ConsumePipelineBuilder UseConsumeErrorStrategy(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => async ctx =>
        {
            var errorStrategy = ctx.ServiceResolver.GetRequiredService<IConsumeErrorStrategy>();
            var logger = ctx.ServiceResolver.GetRequiredService<ILogger<IConsumeErrorStrategy>>();

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

                return AckStrategies.NackWithRequeue;
            }
        });
    }
}
