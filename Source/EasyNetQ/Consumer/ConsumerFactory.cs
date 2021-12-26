using System;
using System.Collections.Concurrent;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Logging;

namespace EasyNetQ.Consumer;

/// <inheritdoc />
public interface IConsumerFactory : IDisposable
{
    /// <summary>
    ///     Creates a consumer based on the configuration
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    IConsumer CreateConsumer(ConsumerConfiguration configuration);
}

/// <inheritdoc />
public class ConsumerFactory : IConsumerFactory
{
    private readonly ConcurrentDictionary<Guid, IConsumer> consumers = new();
    private readonly IEventBus eventBus;
    private readonly ILogger<Consumer> logger;
    private readonly IInternalConsumerFactory internalConsumerFactory;
    private readonly IDisposable unsubscribeFromStoppedConsumerEvent;

    /// <summary>
    ///     Creates ConsumerFactory
    /// </summary>
    public ConsumerFactory(
        ILogger<Consumer> logger,
        IEventBus eventBus,
        IInternalConsumerFactory internalConsumerFactory
    )
    {
        Preconditions.CheckNotNull(logger, nameof(logger));
        Preconditions.CheckNotNull(internalConsumerFactory, nameof(internalConsumerFactory));
        Preconditions.CheckNotNull(eventBus, nameof(eventBus));

        this.logger = logger;
        this.internalConsumerFactory = internalConsumerFactory;
        this.eventBus = eventBus;

        unsubscribeFromStoppedConsumerEvent = eventBus.Subscribe(
            (in StoppedConsumingEvent @event) => consumers.Remove(@event.Consumer.Id)
        );
    }

    /// <inheritdoc />
    public IConsumer CreateConsumer(ConsumerConfiguration configuration)
    {
        var consumer = new Consumer(logger, configuration, internalConsumerFactory, eventBus);
        consumers.TryAdd(consumer.Id, consumer);
        return consumer;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        unsubscribeFromStoppedConsumerEvent.Dispose();
        consumers.ClearAndDispose();
    }
}
