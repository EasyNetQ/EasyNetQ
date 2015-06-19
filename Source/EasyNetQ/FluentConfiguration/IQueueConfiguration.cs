namespace EasyNetQ.FluentConfiguration
{
    /// <summary>
    /// Allows configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithMaxPriority(9)
    /// </summary>
    public interface IQueueConfiguration
    {
        IQueueConfiguration WithAutoDelete(bool autoDelete);

        IQueueConfiguration WithDeadLetterExchange(string deadLetterExchange);

        IQueueConfiguration WithDeadLetterRoutingKey(string deadLetterRoutingKey);

        IQueueConfiguration WithDurable(bool durable);

        IQueueConfiguration WithExclusive(bool exclusive);

        IQueueConfiguration WithExpires(int expires);

        IQueueConfiguration WithMaxPriority(byte maxPriority);

        IQueueConfiguration WithPassive(bool passive);

        IQueueConfiguration WithPerQueueMessageTtl(int perQueueMessageTtl);
    }

    public class QueueConfiguration : IQueueConfiguration
    {
        public bool AutoDelete { get; set; }

        public string DeadLetterExchange { get; set; }

        public string DeadLetterRoutingKey { get; set; }

        public bool Durable { get; set; }

        public bool Exclusive { get; set; }

        public int? Expires { get; set; }

        public byte? MaxPriority { get; set; }

        public bool Passive { get; set; }

        public int? PerQueueMessageTtl { get; set; }

        public QueueConfiguration()
        {
            Durable = true;
        }

        public IQueueConfiguration WithAutoDelete(bool autoDelete)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public IQueueConfiguration WithDeadLetterExchange(string deadLetterExchange)
        {
            DeadLetterExchange = deadLetterExchange;
            return this;
        }

        public IQueueConfiguration WithDeadLetterRoutingKey(string deadLetterRoutingKey)
        {
            DeadLetterRoutingKey = deadLetterRoutingKey;
            return this;
        }

        public IQueueConfiguration WithDurable(bool durable)
        {
            Durable = durable;
            return this;
        }

        public IQueueConfiguration WithExclusive(bool exclusive)
        {
            Exclusive = exclusive;
            return this;
        }

        public IQueueConfiguration WithExpires(int expires)
        {
            Expires = expires;
            return this;
        }

        public IQueueConfiguration WithMaxPriority(byte maxPriority)
        {
            MaxPriority = maxPriority;
            return this;
        }

        public IQueueConfiguration WithPassive(bool passive)
        {
            Passive = passive;
            return this;
        }

        public IQueueConfiguration WithPerQueueMessageTtl(int perQueueMessageTtl)
        {
            PerQueueMessageTtl = perQueueMessageTtl;
            return this;
        }
    }
}
