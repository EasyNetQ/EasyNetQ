using EasyNetQ.ChannelDispatcher;
using EasyNetQ.ConnectionString;
using EasyNetQ.Consumer;
using EasyNetQ.DI;
using EasyNetQ.Interception;
using EasyNetQ.MessageVersioning;
using EasyNetQ.MultipleExchange;
using EasyNetQ.Persistent;
using EasyNetQ.Producer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace EasyNetQ;

public static class ServiceRegisterExtensions
{
    public static void RegisterDefaultServices(
        this IServiceCollection services,
        Func<IServiceProvider, ConnectionConfiguration> connectionConfigurationFactory
    )
    {
        services.TryAddSingleton(resolver =>
        {
            var configuration = connectionConfigurationFactory(resolver);
            configuration.SetDefaultProperties();
            return configuration;
        });
        services.TryAddSingleton<IConnectionStringParser>(
            _ => new CompositeConnectionStringParser(new AmqpConnectionStringParser(), new ConnectionStringParser())
        );
        services.TryAddSingleton<ISerializer>(_ => new ReflectionBasedNewtonsoftJsonSerializer());
        services.TryAddSingleton<IConventions, Conventions>();
        services.TryAddSingleton<IEventBus, EventBus>();
        services.TryAddSingleton<ITypeNameSerializer, DefaultTypeNameSerializer>();
        services.TryAddSingleton<ProducePipelineBuilder>(_ => new ProducePipelineBuilder().UseProduceInterceptors());
        services.TryAddSingleton<ConsumePipelineBuilder>(_ =>
            new ConsumePipelineBuilder().UseConsumeErrorStrategy().UseConsumeInterceptors());
        services.TryAddSingleton<ICorrelationIdGenerationStrategy, DefaultCorrelationIdGenerationStrategy>();
        services.TryAddSingleton<IMessageSerializationStrategy, DefaultMessageSerializationStrategy>();
        services.TryAddSingleton<IMessageDeliveryModeStrategy, MessageDeliveryModeStrategy>();
        services.TryAddSingleton<AdvancedBusEventHandlers>(_ => new AdvancedBusEventHandlers());
        services.TryAddSingleton<IExchangeDeclareStrategy, DefaultExchangeDeclareStrategy>();
        services.TryAddSingleton<IConsumeErrorStrategy, DefaultConsumeErrorStrategy>();
        services.TryAddSingleton<IErrorMessageSerializer, DefaultErrorMessageSerializer>();
        services.TryAddSingleton<IInternalConsumerFactory, InternalConsumerFactory>();
        services.TryAddSingleton<IConsumerFactory, ConsumerFactory>();
        services.TryAddSingleton<IConnectionFactory>(serviceProvider =>
        {
            var connectionConfiguration = serviceProvider.GetRequiredService<ConnectionConfiguration>();
            return ConnectionFactoryFactory.CreateConnectionFactory(connectionConfiguration);
        });
        services.TryAddSingleton<IPersistentChannelDispatcher, SinglePersistentChannelDispatcher>();
        services.TryAddSingleton<IProducerConnection, ProducerConnection>();
        services.TryAddSingleton<IConsumerConnection, ConsumerConnection>();
        services.TryAddSingleton<IPersistentChannelFactory, PersistentChannelFactory>();
        services.TryAddSingleton<IPublishConfirmationListener, PublishConfirmationListener>();
        services.TryAddSingleton<IHandlerCollectionFactory, HandlerCollectionFactory>();
        services.TryAddSingleton<IPullingConsumerFactory, PullingConsumerFactory>();
        services.TryAddSingleton<IAdvancedBus, RabbitAdvancedBus>();
        services.TryAddSingleton<IPubSub, DefaultPubSub>();
        services.TryAddSingleton<IRpc, DefaultRpc>();
        services.TryAddSingleton<ISendReceive, DefaultSendReceive>();
        services.TryAddSingleton<IScheduler, DeadLetterExchangeAndMessageTtlScheduler>();
        services.TryAddSingleton<IBus, RabbitBus>();
        services.TryAddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.TryAddSingleton<ILoggerFactory>(new NullLoggerFactory());
    }

    public static IServiceCollection EnableMultiChannelClientCommandDispatcher(
        this IServiceCollection services, int channelsCount
    )
    {
        return services.AddSingleton<IPersistentChannelDispatcher>(
            x => new MultiPersistentChannelDispatcher(
                channelsCount,
                x.GetRequiredService<IProducerConnection>(),
                x.GetRequiredService<IConsumerConnection>(),
                x.GetRequiredService<IPersistentChannelFactory>()
            )
        );
    }

    public static IServiceCollection EnableLegacyTypeNaming(this IServiceCollection services)
        => services.AddSingleton<ITypeNameSerializer, LegacyTypeNameSerializer>();

    public static IServiceCollection EnableLegacyRpcConventions(this IServiceCollection services)
        => services.AddSingleton<IConventions, LegacyRpcConventions>();

    public static IServiceCollection EnableLegacyConventions(this IServiceCollection services)
    {
        return services
            .EnableLegacyTypeNaming()
            .EnableLegacyRpcConventions();
    }

    public static IServiceCollection EnableDelayedExchangeScheduler(this IServiceCollection services)
        => services.AddSingleton<IScheduler, DelayedExchangeScheduler>();

    public static IServiceCollection EnableAdvancedMessagePolymorphism(this IServiceCollection services)
        => services.AddSingleton<IExchangeDeclareStrategy, MultipleExchangeDeclareStrategy>();

    public static IServiceCollection EnableMessageVersioning(this IServiceCollection services)
    {
        return services
            .AddSingleton<IExchangeDeclareStrategy, VersionedExchangeDeclareStrategy>()
            .AddSingleton<IMessageSerializationStrategy, VersionedMessageSerializationStrategy>();
    }

    public static IServiceCollection EnableConsoleLogger(this IServiceCollection services)
        => services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

    public static IServiceCollection EnableAlwaysAckConsumerErrorStrategy(this IServiceCollection services)
        => services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.Ack);

    public static IServiceCollection EnableAlwaysNackWithRequeueConsumerErrorStrategy(this IServiceCollection services)
        => services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithRequeue);

    public static IServiceCollection EnableAlwaysNackWithoutRequeueConsumerErrorStrategy(this IServiceCollection services)
        => services.AddSingleton<IConsumeErrorStrategy>(SimpleConsumeErrorStrategy.NackWithoutRequeue);

    public static ProducePipelineBuilder UseProduceInterceptors(this ProducePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.ServiceResolver.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>().ToArray();
            var producedMessage = interceptors.OnProduce(new ProducedMessage(ctx.Properties, ctx.Body));
            return next(ctx with { Properties = producedMessage.Properties, Body = producedMessage.Body });
        });
    }

    public static ConsumePipelineBuilder UseConsumeInterceptors(this ConsumePipelineBuilder pipelineBuilder)
    {
        return pipelineBuilder.Use(next => ctx =>
        {
            var interceptors = ctx.ServiceResolver.GetRequiredService<IEnumerable<IProduceConsumeInterceptor>>().ToArray();
            var consumedMessage = interceptors.OnConsume(new ConsumedMessage(ctx.ReceivedInfo, ctx.Properties, ctx.Body));
            return next(ctx with { ReceivedInfo = consumedMessage.ReceivedInfo, Properties = consumedMessage.Properties, Body = consumedMessage.Body });
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
