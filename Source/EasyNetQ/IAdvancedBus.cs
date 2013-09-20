using System;
using System.Threading.Tasks;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    /// IAdvancedBus is a lower level API than IBus which gives you fined grained control
    /// of routing topology, but keeping the EasyNetQ serialisation, persistent connection,
    /// error handling and subscription thread.
    /// </summary>
    public interface IAdvancedBus : IDisposable
    {
        /// <summary>
        /// Consume a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        void Consume<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage) where T : class;

        /// <summary>
        /// Consume raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the 
        /// receive context. Returns a Task.
        /// </param>
        void Consume(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage);

        /// <summary>
        /// Return a channel for publishing.
        /// </summary>
        /// <returns>IAdvancedPublishChannel</returns>
        IAdvancedPublishChannel OpenPublishChannel();

        /// <summary>
        /// Return a channel for publishing.
        /// </summary>
        /// <param name="configure">
        /// Channel configuration e.g. x => x.WithPublisherConfirms()
        /// </param>
        /// <returns>IAdvancedPublishChannel</returns>
        IAdvancedPublishChannel OpenPublishChannel(Action<IChannelConfiguration> configure);

        /// <summary>
        /// Declare a queue. If the queue already exists this method does nothing
        /// </summary>
        /// <param name="name">The name of the queue</param>
        /// <param name="passive">Throw an exception rather than create the queue if it doesn't exist</param>
        /// <param name="durable">Durable queues remain active when a server restarts.</param>
        /// <param name="exclusive">Exclusive queues may only be accessed by the current connection, 
        /// and are deleted when that connection closes.</param>
        /// <param name="autoDelete">If set, the queue is deleted when all consumers have finished using it.</param>
        /// <param name="perQueueTtl">How long a message published to a queue can live before it is discarded by the server.</param>
        /// <param name="expires">Determines how long a queue can remain unused before it is automatically deleted by the server.</param>
        /// <returns>The queue</returns>
        IQueue QueueDeclare(
            string name,
            bool passive = false,
            bool durable = true,
            bool exclusive = false,
            bool autoDelete = false,
            uint perQueueTtl = uint.MaxValue,
            uint expires = uint.MaxValue);

        /// <summary>
        /// Declare a transient server named queue. Note, this queue will only last for duration of the
        /// connection. If there is a connection outage, EasyNetQ will not attempt to recreate
        /// consumers.
        /// </summary>
        /// <returns>The queue</returns>
        IQueue QueueDeclare();

        /// <summary>
        /// Delete a queue
        /// </summary>
        /// <param name="queue">The queue to delete</param>
        /// <param name="ifUnused">Only delete if unused</param>
        /// <param name="ifEmpty">Only delete if empty</param>
        void QueueDelete(IQueue queue, bool ifUnused = false, bool ifEmpty = false);

        /// <summary>
        /// Purget a queue
        /// </summary>
        /// <param name="queue">The queue to purge</param>
        void QueuePurge(IQueue queue);

        /// <summary>
        /// Declare an exchange
        /// </summary>
        /// <param name="name">The exchange name</param>
        /// <param name="type">The type of exchange</param>
        /// <param name="passive">Throw an exception rather than create the exchange if it doens't exist</param>
        /// <param name="durable">Durable exchanges remain active when a server restarts.</param>
        /// <param name="autoDelete">If set, the exchange is deleted when all queues have finished using it.</param>
        /// <param name="internal">If set, the exchange may not be used directly by publishers, 
        /// but only when bound to other exchanges.</param>
        /// <returns>The exchange</returns>
        IExchange ExchangeDeclare(
            string name, 
            string type, 
            bool passive = false, 
            bool durable = true, 
            bool autoDelete = false, 
            bool @internal = false);

        /// <summary>
        /// Delete an exchange
        /// </summary>
        /// <param name="exchange">The exchange to delete</param>
        /// <param name="ifUnused">If set, the server will only delete the exchange if it has no queue bindings.</param>
        void ExchangeDelete(IExchange exchange, bool ifUnused = false);

        /// <summary>
        /// Bind an exchange to a queue. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="exchange">The exchange to bind</param>
        /// <param name="queue">The queue to bind</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange exchange, IQueue queue, string routingKey);

        /// <summary>
        /// Bind two exchanges. Does nothing if the binding already exists.
        /// </summary>
        /// <param name="source">The source exchange</param>
        /// <param name="destination">The destination exchange</param>
        /// <param name="routingKey">The routing key</param>
        /// <returns>A binding</returns>
        IBinding Bind(IExchange source, IExchange destination, string routingKey);

        /// <summary>
        /// Delete a binding
        /// </summary>
        /// <param name="binding">the binding to delete</param>
        void BindingDelete(IBinding binding);

        /// <summary>
        /// True if the bus is connected, False if it is not.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The message serializer
        /// </summary>
        ISerializer Serializer { get; }

        /// <summary>
        /// How EasyNetQ stringifies the message type
        /// </summary>
        SerializeType SerializeType { get; }

        /// <summary>
        /// Event fires when the bus connects
        /// </summary>
        event Action Connected;

        /// <summary>
        /// Event fires when the bus disconnects
        /// </summary>
        event Action Disconnected;
    }
}