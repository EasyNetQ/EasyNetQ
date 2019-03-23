using System;
using System.Collections.Generic;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public static class QueueDeclareConfigurationActions
    {
        /// <summary>
        /// Create the action to configure queue
        /// </summary>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="perQueueMessageTtl">Determines how long a message published to a queue can live before it is discarded by the server.</param>
        /// <param name="expires">Determines how long a queue can remain unused before it is automatically deleted by the server.</param>
        /// <param name="maxPriority">Determines the maximum message priority that the queue should support.</param>
        /// <param name="deadLetterExchange">Determines an exchange's name can remain unused before it is automatically deleted by the server.</param>
        /// <param name="deadLetterRoutingKey">If set, will route message with the routing key specified, if not set, message will be routed with the same routing keys they were originally published with.</param>
        /// <param name="maxLength">The maximum number of ready messages that may exist on the queue.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <param name="maxLengthBytes">The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached</param>
        /// <returns>
        /// The configuration action
        /// </returns>
        public static Action<IQueueDeclareConfiguration> From(
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            int? perQueueMessageTtl = null,
            int? expires = null,
            int? maxPriority = null,
            string deadLetterExchange = null,
            string deadLetterRoutingKey = null,
            int? maxLength = null,
            int? maxLengthBytes = null
        )
        {
            return c =>
            {
                c.AsDurable(durable);
                c.AsExclusive(exclusive);
                c.AsAutoDelete(autoDelete);
                if (perQueueMessageTtl.HasValue) c.WithMessageTtl(TimeSpan.FromMilliseconds(perQueueMessageTtl.Value));
                if (expires.HasValue) c.WithExpires(TimeSpan.FromMilliseconds(expires.Value));
                if (maxPriority.HasValue) c.WithMaxPriority(maxPriority.Value);
                if (deadLetterExchange != null) c.WithDeadLetterExchange(new Exchange(deadLetterExchange));
                if (deadLetterRoutingKey != null) c.WithDeadLetterRoutingKey(deadLetterRoutingKey);
                if (maxLength.HasValue) c.WithMaxLength(maxLength.Value);
                if (maxLengthBytes.HasValue) c.WithMaxLengthBytes(maxLengthBytes.Value);
            };
        }
    }

    /// <summary>
    /// Allows queue declaration configuration to be fluently extended without adding overloads to IBus
    /// 
    /// e.g.
    /// x => x.WithMaxPriority(42)
    /// </summary>
    public interface IQueueDeclareConfiguration
    {
        /// <summary>
        /// Sets as durable or not. Durable queues remain active when a server restarts.
        /// </summary>
        /// <param name="durable">The durable flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsDurable(bool durable = true);

        /// <summary>
        /// Sets as exclusive or not. Exclusive queues may only be accessed by the current connection, and are deleted when that connection closes.
        /// </summary>
        /// <param name="exclusive">The exclusive flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsExclusive(bool exclusive = true);

        /// <summary>
        /// Sets as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="autoDelete">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration AsAutoDelete(bool autoDelete = true);

        /// <summary>
        /// Sets queue as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="maxPriority">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithMaxPriority(int maxPriority);

        /// <summary>
        /// Sets maxLength. The maximum number of ready messages that may exist on the queue. Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="maxLength">The maxLength to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithMaxLength(int maxLength);

        /// <summary>
        /// Sets maxLengthBytes. The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="maxLengthBytes">The maxLengthBytes flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithMaxLengthBytes(int maxLengthBytes);

        /// <summary>
        /// Sets expires of the queue. Determines how long a queue can remain unused before it is automatically deleted by the server.
        /// </summary>
        /// <param name="expires">The autoDelete flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithExpires(TimeSpan expires);

        /// <summary>
        /// Sets messageTtl. Determines how long a message published to a queue can live before it is discarded by the server.
        /// </summary>
        /// <param name="messageTtl">The messageTtl to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithMessageTtl(TimeSpan messageTtl);

        /// <summary>
        /// Sets deadLetterExchange. Determines an exchange's name can remain unused before it is automatically deleted by the server.
        /// </summary>
        /// <param name="deadLetterExchange">The deadLetterExchange to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithDeadLetterExchange(IExchange deadLetterExchange);

        /// <summary>
        /// Sets deadLetterRoutingKey. If set, will route message with the routing key specified, if not set, message will be routed with the same routing keys they were originally published with.
        /// </summary>
        /// <param name="deadLetterRoutingKey">The deadLetterRoutingKey to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithDeadLetterRoutingKey(string deadLetterRoutingKey);

        /// <summary>
        /// Sets queueMode. Valid modes are default and lazy.
        /// </summary>
        /// <param name="queueMode">The queueMode to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithQueueMode(string queueMode = QueueMode.Default);

        /// <summary>
        /// Sets a raw argument for query declaration
        /// </summary>
        /// <param name="name">The argument name to set</param>
        /// <param name="value">The argument value to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        IQueueDeclareConfiguration WithArgument(string name, object value);
    }

    public class QueueDeclareConfiguration : IQueueDeclareConfiguration
    {
        public bool Durable { get; private set; } = true;
        public bool Exclusive { get; private set; }
        public bool AutoDelete { get; private set; }

        public IDictionary<string, object> Arguments { get; } = new Dictionary<string, object>();

        public IQueueDeclareConfiguration AsDurable(bool durable = true)
        {
            Durable = durable;
            return this;
        }

        public IQueueDeclareConfiguration AsExclusive(bool exclusive = true)
        {
            Exclusive = exclusive;
            return this;
        }

        public IQueueDeclareConfiguration AsAutoDelete(bool autoDelete = true)
        {
            AutoDelete = autoDelete;
            return this;
        }

        public IQueueDeclareConfiguration WithMaxPriority(int maxPriority)
        {
            return WithArgument("x-max-priority", maxPriority);
        }

        public IQueueDeclareConfiguration WithMaxLength(int maxLength)
        {
            return WithArgument("x-max-length", maxLength);
        }

        public IQueueDeclareConfiguration WithMaxLengthBytes(int maxLengthBytes)
        {
            return WithArgument("x-max-length-bytes", maxLengthBytes);
        }

        public IQueueDeclareConfiguration WithExpires(TimeSpan expires)
        {
            return WithArgument("x-expires", (int) expires.TotalMilliseconds);
        }

        public IQueueDeclareConfiguration WithMessageTtl(TimeSpan messageTtl)
        {
            return WithArgument("x-message-ttl", (int) messageTtl.TotalMilliseconds);
        }

        public IQueueDeclareConfiguration WithDeadLetterExchange(IExchange deadLetterExchange)
        {
            return WithArgument("x-dead-letter-exchange", deadLetterExchange.Name);
        }

        public IQueueDeclareConfiguration WithDeadLetterRoutingKey(string deadLetterRoutingKey)
        {
            return WithArgument("x-dead-letter-routing-key", deadLetterRoutingKey);
        }

        public IQueueDeclareConfiguration WithQueueMode(string queueMode = QueueMode.Default)
        {
            return WithArgument("x-queue-mode", queueMode);
        }

        public IQueueDeclareConfiguration WithArgument(string name, object value)
        {
            Arguments[name] = value;
            return this;
        }
    }
}
