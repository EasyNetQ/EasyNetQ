using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ;

internal class PerQueueConsumeConfiguration : IPerQueueConsumeConfiguration
{
    public bool AutoAck { get; private set; }

    public string ConsumerTag { get; private set; } = "";

    public bool IsExclusive { get; private set; }

    public IDictionary<string, object>? Arguments { get; private set; }


    public IPerQueueConsumeConfiguration WithConsumerTag(string consumerTag)
    {
        ConsumerTag = consumerTag;
        return this;
    }

    public IPerQueueConsumeConfiguration WithExclusive(bool isExclusive = true)
    {
        IsExclusive = isExclusive;
        return this;
    }

    public IPerQueueConsumeConfiguration WithArgument(string name, object value)
    {
        (Arguments ??= new Dictionary<string, object>())[name] = value;
        return this;
    }

    public IPerQueueConsumeConfiguration WithAutoAck()
    {
        AutoAck = true;
        return this;
    }
}

internal class ConsumeConfiguration : IConsumeConfiguration
{
    private readonly IHandlerCollectionFactory handlerCollectionFactory;

    public ConsumeConfiguration(
        ushort defaultPrefetchCount, IHandlerCollectionFactory handlerCollectionFactory
    )
    {
        this.handlerCollectionFactory = handlerCollectionFactory;
        PrefetchCount = defaultPrefetchCount;
        PerQueueConsumeConfigurations = new();
        PerQueueTypedConsumeConfigurations = new();
    }

    public ushort PrefetchCount { get; private set; }
    public ushort? ConsumerDispatcherConcurrency { get; private set; }
    public List<Tuple<Queue, MessageHandler, PerQueueConsumeConfiguration>> PerQueueConsumeConfigurations { get; }

    public List<Tuple<Queue, IHandlerCollection, PerQueueConsumeConfiguration>> PerQueueTypedConsumeConfigurations { get; }

    public IConsumeConfiguration WithPrefetchCount(ushort prefetchCount)
    {
        PrefetchCount = prefetchCount;
        return this;
    }

    public IConsumeConfiguration WithConsumerDispatcherConcurrency(ushort consumerDispatcherConcurrency)
    {
        ConsumerDispatcherConcurrency = consumerDispatcherConcurrency;
        return this;
    }

    public IConsumeConfiguration ForQueue(
        in Queue queue, MessageHandler handler, Action<IPerQueueConsumeConfiguration> configure
    )
    {
        var perQueueConsumeConfiguration = new PerQueueConsumeConfiguration();
        configure(perQueueConsumeConfiguration);
        PerQueueConsumeConfigurations.Add(Tuple.Create(queue, handler, perQueueConsumeConfiguration));
        return this;
    }

    public IConsumeConfiguration ForQueue(
        in Queue queue, Action<IHandlerRegistration> register, Action<IPerQueueConsumeConfiguration> configure
    )
    {
        var handlerCollection = handlerCollectionFactory.CreateHandlerCollection(queue);
        register(handlerCollection);
        var perQueueConsumeConfiguration = new PerQueueConsumeConfiguration();
        configure(perQueueConsumeConfiguration);
        PerQueueTypedConsumeConfigurations.Add(Tuple.Create(queue, handlerCollection, perQueueConsumeConfiguration));
        return this;
    }
}

/// <summary>
/// Allows consumer per queue configuration to be fluently extended without adding overloads to IBus
///
/// e.g.
/// x => x.WithExclusive()
/// </summary>
public interface IPerQueueConsumeConfiguration
{
    /// <summary>
    ///     Automatically acknowledge a message
    /// </summary>
    /// <returns></returns>
    IPerQueueConsumeConfiguration WithAutoAck();

    /// <summary>
    ///     Sets consumer tag
    /// </summary>
    /// <param name="consumerTag">The consumerTag to set</param>
    /// <returns>IPerQueueConsumeConfiguration</returns>
    IPerQueueConsumeConfiguration WithConsumerTag(string consumerTag);

    /// <summary>
    ///     Switch a consumer to exclusive mode
    /// </summary>
    /// <returns>IPerQueueConsumeConfiguration</returns>
    IPerQueueConsumeConfiguration WithExclusive(bool isExclusive = true);

    /// <summary>
    ///     Sets a raw argument for consumer declaration
    /// </summary>
    /// <param name="name">The argument name to set</param>
    /// <param name="value">The argument value to set</param>
    /// <returns>IPerQueueConsumeConfiguration</returns>
    IPerQueueConsumeConfiguration WithArgument(string name, object value);
}

/// <summary>
/// Allows consumer configuration to be fluently extended without adding overloads to IBus
///
/// e.g.
/// x => x.WithPrefetchCount(42)
/// </summary>
public interface IConsumeConfiguration
{
    /// <summary>
    ///     Sets prefetch count
    /// </summary>
    /// <param name="prefetchCount">The prefetchCount to set</param>
    /// <returns>IConsumerConfiguration</returns>
    IConsumeConfiguration WithPrefetchCount(ushort prefetchCount);

    /// <summary>
    ///     Sets consumer dispatcher concurrency, overriding <see cref="ConnectionConfiguration.ConsumerDispatcherConcurrency"/>
    ///     for this consumer. It is shared by all the queues of the consumer, because they are consumed on one channel
    /// </summary>
    /// <remarks>
    ///     The messages handled at the same time are also limited by the prefetch count of each queue, unless it is zero
    ///     (unlimited) or messages are auto acknowledged.
    ///     For concurrency greater than one, the consumer could process messages in any order, not in the order it receives them
    /// </remarks>
    /// <param name="consumerDispatcherConcurrency">The consumerDispatcherConcurrency to set, greater than zero</param>
    /// <returns>IConsumeConfiguration</returns>
    IConsumeConfiguration WithConsumerDispatcherConcurrency(ushort consumerDispatcherConcurrency);

    /// <summary>
    ///     Add consume configuration for a given queue
    /// </summary>
    /// <returns>IConsumeConfiguration</returns>
    IConsumeConfiguration ForQueue(
        in Queue queue, MessageHandler handler, Action<IPerQueueConsumeConfiguration> configure
    );

    /// <summary>
    ///     Add consume configuration for a given queue
    /// </summary>
    /// <returns>IConsumeConfiguration</returns>
    IConsumeConfiguration ForQueue(
        in Queue queue, Action<IHandlerRegistration> register, Action<IPerQueueConsumeConfiguration> configure
    );
}



