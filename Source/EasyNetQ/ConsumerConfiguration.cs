using System.Collections.Generic;

namespace EasyNetQ
{
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
        ///     Indicates whether a consumer is in exclusive mode or not
        /// </summary>
        public bool IsExclusive { get; private set; }

        /// <summary>
        ///     Prefetch count
        /// </summary>
        public ushort PrefetchCount { get; private set; }

        /// <summary>
        ///     Arguments
        /// </summary>
        public IDictionary<string, object> Arguments { get; private set; }

        /// <inheritdoc />
        public IConsumerConfiguration WithArgument(string name, object value)
        {
            (Arguments ??= new Dictionary<string, object>())[name] = value;
            return this;
        }

        /// <inheritdoc />
        public IConsumerConfiguration WithExclusive(bool isExclusive = true)
        {
            IsExclusive = isExclusive;
            return this;
        }

        /// <inheritdoc />
        public IConsumerConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }
    }

    /// <summary>
    /// Allows consumer configuration to be fluently extended without adding overloads to IBus
    ///
    /// e.g.
    /// x => x.WithPrefetchCount(42)
    /// </summary>
    public interface IConsumerConfiguration
    {
        /// <summary>
        ///     Switch a consumer to exclusive mode
        /// </summary>
        /// <returns>IConsumerConfiguration</returns>
        IConsumerConfiguration WithExclusive(bool isExclusive = true);

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
}
