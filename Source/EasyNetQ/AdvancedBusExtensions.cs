using System.Collections.Generic;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    public static class AdvancedBusExtensions
    {
        /// <summary>
        ///     Publish a message as a byte array
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        ///     The routing key for the message. The routing key is used for routing messages depending on the
        ///     exchange configuration.
        /// </param>
        /// <param name="mandatory">
        ///     This flag tells the server how to react if the message cannot be routed to a queue.
        ///     If this flag is true, the server will return an unroutable message with a Return method.
        ///     If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="messageProperties">The message properties</param>
        /// <param name="body">The message body</param>
        public static void Publish(
            this IAdvancedBus bus,
            IExchange exchange,
            string routingKey,
            bool mandatory,
            MessageProperties messageProperties,
            byte[] body
        )
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(exchange, routingKey, mandatory, messageProperties, body)
               .GetAwaiter()
               .GetResult();
        }

        /// <summary>
        ///     Publish a message as a .NET type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">
        ///     The routing key for the message. The routing key is used for routing messages depending on the
        ///     exchange configuration.
        /// </param>
        /// <param name="mandatory">
        ///     This flag tells the server how to react if the message cannot be routed to a queue.
        ///     If this flag is true, the server will return an unroutable message with a Return method.
        ///     If this flag is false, the server silently drops the message.
        /// </param>
        /// <param name="message">The message to publish</param>
        public static void Publish<T>(
            this IAdvancedBus bus,
            IExchange exchange,
            string routingKey,
            bool mandatory,
            IMessage<T> message
        ) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.PublishAsync(exchange, routingKey, mandatory, message)
               .GetAwaiter()
               .GetResult();
        }

        /// <summary>
        /// Get a message from the given queue.
        /// </summary>
        /// <typeparam name="T">The message type to get</typeparam>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue from which to retreive the message</param>
        /// <returns>An IBasicGetResult.</returns>
        public static IBasicGetResult<T> GetMessage<T>(this IAdvancedBus bus, IQueue queue) where T : class
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.GetMessageAsync<T>(queue)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Get the raw message from the given queue.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue from which to retreive the message</param>
        /// <returns>An IBasicGetResult</returns>
        public static IBasicGetResult GetMessage(this IAdvancedBus bus, IQueue queue)
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.GetMessageAsync(queue)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Counts messages in the given queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue in which to count messages</param>
        /// <returns>The number of counted messages</returns>
        public static uint GetMessagesCount(this IAdvancedBus bus, IQueue queue)
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.GetMessagesCountAsync(queue)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <returns>The queue</returns>
        public static IQueue QueueDeclare(this IAdvancedBus bus)
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.QueueDeclareAsync()
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The name of the queue</param>
        /// <param name="passive">Throw an exception rather than create the queue if it doesn't exist</param>
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
        /// The queue
        /// </returns>
        public static IQueue QueueDeclare(
            this IAdvancedBus bus,
            string name,
            bool passive = false,
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
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.QueueDeclareAsync(name, passive, durable, exclusive, autoDelete, perQueueMessageTtl, expires, maxPriority, deadLetterExchange, deadLetterRoutingKey, maxLength, maxLengthBytes)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        public static IBinding Bind(this IAdvancedBus bus, IExchange source, IExchange destination, string routingKey)
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(source, destination, routingKey)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        public static IBinding Bind(
            this IAdvancedBus bus,
            IExchange source,
            IExchange destination,
            string routingKey,
            IDictionary<string, object> headers
        )
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.BindAsync(source, destination, routingKey, headers)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        public static IBinding Bind(this IAdvancedBus bus, IExchange exchange, IQueue queue, string routingKey)
        {
            Preconditions.CheckNotNull(bus, "bus");

            return bus.BindAsync(exchange, queue, routingKey)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="headers">The headers</param>
        /// <returns>A binding</returns>
        public static IBinding Bind(
            this IAdvancedBus bus,
            IExchange exchange, 
            IQueue queue,
            string routingKey,
            IDictionary<string, object> headers
        )
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.BindAsync(exchange, queue, routingKey, headers)
                .GetAwaiter()
                .GetResult();
        }


        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doens't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="alternateExchange">Route messages to this exchange if they cannot be routed.</param>
        /// <param name="delayed">If set, declars x-delayed-type exchange for routing delayed messages.</param>
        /// <returns>The exchange</returns>
        public static IExchange ExchangeDeclare(
            this IAdvancedBus bus,
            string name,
            string type,
            bool passive = false,
            bool durable = true,
            bool autoDelete = false,
            string alternateExchange = null,
            bool delayed = false
        )
        {
            Preconditions.CheckNotNull(bus, "bus");
            
            return bus.ExchangeDeclareAsync(name, type, passive, durable, autoDelete, alternateExchange, delayed)
                      .GetAwaiter()
                      .GetResult();
        }

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="binding">the binding to delete</param>
        public static void Unbind(this IAdvancedBus bus, IBinding binding)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.UnbindAsync(binding)
               .GetAwaiter()
               .GetResult();
        }

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        public static void QueueDelete(this IAdvancedBus bus, IQueue queue, bool ifUnused = false, bool ifEmpty = false)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.QueueDeleteAsync(queue, ifUnused, ifEmpty)
               .GetAwaiter()
               .GetResult();
        }

        /// <summary>
        /// Purges a queue
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="queue">The queue to purge</param>
        public static void QueuePurge(this IAdvancedBus bus, IQueue queue)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.QueuePurgeAsync(queue)
               .GetAwaiter()
               .GetResult();
        }

        /// <summary>
        /// Delete an exchange
        /// </summary>
        /// <param name="bus">The bus instance</param>
        /// <param name="exchange">The exchange to delete</param>
        /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
        public static void ExchangeDelete(this IAdvancedBus bus, IExchange exchange, bool ifUnused = false)
        {
            Preconditions.CheckNotNull(bus, "bus");

            bus.ExchangeDeleteAsync(exchange, ifUnused)
               .GetAwaiter()
               .GetResult();
        }
    }
}