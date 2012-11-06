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
        /// Subscribe to a stream of messages
        /// </summary>
        /// <typeparam name="T">The message type</typeparam>
        /// <param name="queue">The queue to take messages from</param>
        /// <param name="onMessage">The message handler</param>
        void Subscribe<T>(IQueue queue, Func<IMessage<T>, MessageReceivedInfo, Task> onMessage);

        /// <summary>
        /// Subscribe to raw bytes from the queue.
        /// </summary>
        /// <param name="queue">The queue to subscribe to</param>
        /// <param name="onMessage">
        /// The message handler. Takes the message body, message properties and some information about the 
        /// receive context. Returns a Task.
        /// </param>
        void Subscribe(IQueue queue, Func<Byte[], MessageProperties, MessageReceivedInfo, Task> onMessage);

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