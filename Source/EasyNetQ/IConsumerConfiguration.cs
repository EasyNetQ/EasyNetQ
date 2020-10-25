using System.Collections.Generic;

namespace EasyNetQ
{
    /// <summary>
    /// Allows consumer configuration to be fluently extended without adding overloads to IBus
    ///
    /// e.g.
    /// x => x.WithPrefetchCount(42)
    /// </summary>
    public interface IConsumerConfiguration
    {
        /// <summary>
        /// Sets consumer tag
        /// </summary>
        /// <param name="consumerTag">The consumerTag to set</param>
        /// <returns>IConsumerConfiguration</returns>
        IConsumerConfiguration WithConsumerTag(string consumerTag);

        /// <summary>
        /// Sets prefetch count
        /// </summary>
        /// <param name="prefetchCount">The prefetchCount to set</param>
        /// <returns>IConsumerConfiguration</returns>
        IConsumerConfiguration WithPrefetchCount(ushort prefetchCount);

        /// <summary>
        /// Sets a raw argument for consumer declaration
        /// </summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>IConsumerConfiguration</returns>
        IConsumerConfiguration WithArgument(string name, object value);
    }

    /// <inheritdoc />
    public class ConsumerConfiguration : IConsumerConfiguration
    {
        /// <summary>
        ///     Create ConsumerConfiguration
        /// </summary>
        /// <param name="defaultPrefetchCount"></param>
        public ConsumerConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        /// <summary>
        ///     Consumer tag
        /// </summary>
        public string ConsumerTag { get; private set; } = "";


        /// <summary>
        ///     Prefetch count
        /// </summary>
        public ushort PrefetchCount { get; private set; }


        /// <summary>
        ///     Arguments
        /// </summary>
        public IDictionary<string, object> Arguments { get; private set; }

        /// <inheritdoc />
        public IConsumerConfiguration WithConsumerTag(string consumerTag)
        {
            Preconditions.CheckNotNull(consumerTag, nameof(consumerTag));
            ConsumerTag = consumerTag;
            return this;
        }

        /// <inheritdoc />
        public IConsumerConfiguration WithArgument(string name, object value)
        {
            (Arguments ??= new Dictionary<string, object>())[name] = value;
            return this;
        }

        /// <inheritdoc />
        public IConsumerConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }
    }
}
