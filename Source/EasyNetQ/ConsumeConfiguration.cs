using System;
using System.Collections.Generic;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    internal class PerQueueConsumeConfiguration : IPerQueueConsumeConfiguration
    {
        public string ConsumerTag { get; private set; } = "";

        public bool IsExclusive { get; private set; }

        public IDictionary<string, object> Arguments { get; private set; }

        public IPerQueueConsumeConfiguration WithConsumerTag(string consumerTag)
        {
            Preconditions.CheckNotNull(consumerTag, nameof(consumerTag));
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
    }

    internal class ConsumeConfiguration : IConsumeConfiguration
    {
        private readonly Func<Queue, IHandlerCollection> createHandlerCollection;

        public ConsumeConfiguration(
            ushort defaultPrefetchCount, Func<Queue, IHandlerCollection> createHandlerCollection
        )
        {
            this.createHandlerCollection = createHandlerCollection;
            PrefetchCount = defaultPrefetchCount;
            PerQueueConsumeConfigurations = new List<Tuple<Queue, MessageHandler, PerQueueConsumeConfiguration>>();
            PerQueueTypedConsumeConfigurations =
                new List<Tuple<Queue, IHandlerCollection, PerQueueConsumeConfiguration>>();
        }

        public ushort PrefetchCount { get; private set; }
        public List<Tuple<Queue, MessageHandler, PerQueueConsumeConfiguration>> PerQueueConsumeConfigurations { get; }

        public List<Tuple<Queue, IHandlerCollection, PerQueueConsumeConfiguration>> PerQueueTypedConsumeConfigurations
        {
            get;
        }

        public IConsumeConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }

        public IConsumeConfiguration ForQueue(
            Queue queue, MessageHandler handler, Action<IPerQueueConsumeConfiguration> configure
        )
        {
            var perQueueConsumeConfiguration = new PerQueueConsumeConfiguration();
            configure(perQueueConsumeConfiguration);
            PerQueueConsumeConfigurations.Add(Tuple.Create(queue, handler, perQueueConsumeConfiguration));
            return this;
        }

        public IConsumeConfiguration ForQueue(
            Queue queue, Action<IHandlerRegistration> register, Action<IPerQueueConsumeConfiguration> configure
        )
        {
            var handlerCollection = createHandlerCollection(queue);
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
        /// Sets consumer tag
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
        /// Sets a raw argument for consumer declaration
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
        ///     Add consume configuration for a given queue
        /// </summary>
        /// <returns>IConsumeConfiguration</returns>
        IConsumeConfiguration ForQueue(
            Queue queue, MessageHandler handler, Action<IPerQueueConsumeConfiguration> configure
        );

        /// <summary>
        ///     Add consume configuration for a given queue
        /// </summary>
        /// <returns>IConsumeConfiguration</returns>
        IConsumeConfiguration ForQueue(
            Queue queue, Action<IHandlerRegistration> register, Action<IPerQueueConsumeConfiguration> configure
        );
    }

    /// <summary>
    /// Allows consumer configuration to be fluently extended without adding overloads to IBus
    ///
    /// e.g.
    /// x => x.WithPrefetchCount(42)
    /// </summary>
    public interface ISimpleConsumeConfiguration
    {
        /// <summary>
        /// Sets consumer tag
        /// </summary>
        /// <param name="consumerTag">The consumerTag to set</param>
        /// <returns>IPerQueueConsumeConfiguration</returns>
        ISimpleConsumeConfiguration WithConsumerTag(string consumerTag);

        /// <summary>
        ///     Switch a consumer to exclusive mode
        /// </summary>
        /// <returns>IPerQueueConsumeConfiguration</returns>
        ISimpleConsumeConfiguration WithExclusive(bool isExclusive = true);

        /// <summary>
        /// Sets a raw argument for consumer declaration
        /// </summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>IPerQueueConsumeConfiguration</returns>
        ISimpleConsumeConfiguration WithArgument(string name, object value);

        /// <summary>
        ///     Sets prefetch count
        /// </summary>
        /// <param name="prefetchCount">The prefetchCount to set</param>
        /// <returns>IConsumerConfiguration</returns>
        ISimpleConsumeConfiguration WithPrefetchCount(ushort prefetchCount);
    }
}
