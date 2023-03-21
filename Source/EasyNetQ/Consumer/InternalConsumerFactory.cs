using EasyNetQ.DI;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer;

public interface IInternalConsumerFactory
{
    /// <summary>
    ///     Creates a consumer based on the configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    IInternalConsumer CreateConsumer(ConsumerConfiguration configuration);
}

/// <inheritdoc />
public class InternalConsumerFactory : IInternalConsumerFactory
{
    private readonly IServiceResolver serviceResolver;
    private readonly ILogger<InternalConsumer> logger;
    private readonly IConsumerConnection connection;
    private readonly IEventBus eventBus;

    /// <summary>
    ///     Creates InternalConsumerFactory
    /// </summary>
    public InternalConsumerFactory(
        IServiceResolver serviceResolver,
        ILogger<InternalConsumer> logger,
        IConsumerConnection connection,
        IEventBus eventBus
    )
    {
        this.serviceResolver = serviceResolver;
        this.logger = logger;
        this.connection = connection;
        this.eventBus = eventBus;
    }

    /// <inheritdoc />
    public IInternalConsumer CreateConsumer(ConsumerConfiguration configuration)
        => new InternalConsumer(serviceResolver, logger, configuration, connection, eventBus);
}
