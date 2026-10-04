using Microsoft.Extensions.Logging;
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
    private readonly IServiceProvider services;
    private readonly ILogger<InternalConsumer> logger;
    private readonly IConsumerConnection connection;
    private readonly IEventBus eventBus;

    /// <summary>
    ///     Creates InternalConsumerFactory
    /// </summary>
    public InternalConsumerFactory(
        IServiceProvider services,
        ILogger<InternalConsumer> logger,
        IConsumerConnection connection,
        IEventBus eventBus
    )
    {
        this.services = services;
        this.logger = logger;
        this.connection = connection;
        this.eventBus = eventBus;
    }

    /// <inheritdoc />
    public IInternalConsumer CreateConsumer(ConsumerConfiguration configuration)
        => new InternalConsumer(services, logger, configuration, connection, eventBus);
}
