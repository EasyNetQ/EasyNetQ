using System;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IQueueDeclareConfiguration
    /// </summary>
    public static class QueueDeclareConfigurationExtensions
    {
        /// <summary>
        ///     Sets queue as autoDelete or not. If set, the queue is deleted when all consumers have finished using it.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="maxPriority">The maxPriority to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithMaxPriority(this IQueueDeclareConfiguration configuration, int maxPriority)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-max-priority", maxPriority);
        }

        /// <summary>
        ///     Sets maxLength. The maximum number of ready messages that may exist on the queue. Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="maxLength">The maxLength to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithMaxLength(this IQueueDeclareConfiguration configuration, int maxLength)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-max-length", maxLength);
        }

        /// <summary>
        ///     Sets maxLengthBytes. The maximum size of the queue in bytes.  Messages will be dropped or dead-lettered from the front of the queue to make room for new messages once the limit is reached.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="maxLengthBytes">The maxLengthBytes flag to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithMaxLengthBytes(this IQueueDeclareConfiguration configuration, int maxLengthBytes)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-max-length-bytes", maxLengthBytes);
        }

        /// <summary>
        ///     Sets expires of the queue. Determines how long a queue can remain unused before it is automatically deleted by the server.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="expires">The expires to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithExpires(this IQueueDeclareConfiguration configuration, TimeSpan expires)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-expires", (int)expires.TotalMilliseconds);
        }

        /// <summary>
        ///     Sets messageTtl. Determines how long a message published to a queue can live before it is discarded by the server.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="messageTtl">The messageTtl to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithMessageTtl(this IQueueDeclareConfiguration configuration, TimeSpan messageTtl)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-message-ttl", (int)messageTtl.TotalMilliseconds);
        }

        /// <summary>
        ///     Sets deadLetterExchange. Determines an exchange's name can remain unused before it is automatically deleted by the server.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="deadLetterExchange">The deadLetterExchange to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithDeadLetterExchange(this IQueueDeclareConfiguration configuration, IExchange deadLetterExchange)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-dead-letter-exchange", deadLetterExchange.Name);
        }

        /// <summary>
        ///     Sets deadLetterRoutingKey. If set, will route message with the routing key specified, if not set, message will be routed with the same routing keys they were originally published with.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="deadLetterRoutingKey">The deadLetterRoutingKey to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithDeadLetterRoutingKey(this IQueueDeclareConfiguration configuration, string deadLetterRoutingKey)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-dead-letter-routing-key", deadLetterRoutingKey);
        }

        /// <summary>
        ///     Sets queueMode. Valid modes are default and lazy.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="queueMode">The queueMode to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithQueueMode(this IQueueDeclareConfiguration configuration, string queueMode = QueueMode.Default)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-queue-mode", queueMode);
        }

        /// <summary>
        ///     Sets queueType. Valid types are classic and quorum.
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="queueType">The queueType to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithQueueType(this IQueueDeclareConfiguration configuration, string queueType = QueueType.Classic)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-queue-type", queueType);
        }

        /// <summary>
        ///     Enables single active consumer
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IQueueDeclareConfiguration WithSingleActiveConsumer(this IQueueDeclareConfiguration configuration)
        {
            Preconditions.CheckNotNull(configuration, "configuration");

            return configuration.WithArgument("x-single-active-consumer", true);
        }
    }
}
