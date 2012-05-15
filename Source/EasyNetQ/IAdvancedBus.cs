using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    /// IAdvancedBus is a lower level API than IBus which gives you fined grained control
    /// of routing topology, but keeping the EasyNetQ serialisation, persistent connection,
    /// error handling and subscription thread.
    /// </summary>
    public interface IAdvancedBus
    {
        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="exchange">The exchange to publish to</param>
        /// <param name="routingKey">The routing key</param>
        /// <param name="message">The message to publish</param>
        void Publish<T>(IExchange exchange, string routingKey, IMessage<T> message);

        /// <summary>
        /// Subscribe to a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        void Subscribe<T>(IQueue queue, Func<IMessage<T>, Task> onMessage);
    }
}