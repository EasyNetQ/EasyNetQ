using System.Collections.Generic;

namespace EasyNetQ.Consumer
{
    public interface IConsumerConfiguration
    {
        IConsumerConfiguration WithConsumerTag(string consumerTag);
        IConsumerConfiguration WithPrefetchCount(ushort prefetchCount);
        IConsumerConfiguration WithArgument(string name, object value);
    }

    public class ConsumerConfiguration : IConsumerConfiguration
    {
        public ConsumerConfiguration(ushort defaultPrefetchCount)
        {
            PrefetchCount = defaultPrefetchCount;
        }

        public string ConsumerTag { get; private set; } = "";
        public ushort PrefetchCount { get; private set; }
        public IDictionary<string, object> Arguments { get; private set; }

        public IConsumerConfiguration WithConsumerTag(string consumerTag)
        {
            Preconditions.CheckNotNull(consumerTag, nameof(consumerTag));
            ConsumerTag = consumerTag;
            return this;
        }

        public IConsumerConfiguration WithArgument(string name, object value)
        {
            (Arguments ??= new Dictionary<string, object>())[name] = value;
            return this;
        }

        public IConsumerConfiguration WithPrefetchCount(ushort prefetchCount)
        {
            PrefetchCount = prefetchCount;
            return this;
        }
    }
}
