using System;

namespace EasyNetQ
{
    /// <summary>
    /// Represents a handler container for the events that exist in <see cref="IAdvancedBus"/> and <see cref="IBus"/>.
    /// </summary>
    public interface IBusEventHandlers
    {
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Connected"/> and <see cref="IBus.Connected"/>.
        /// </summary>
        Action Connected { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Disconnected"/> and <see cref="IBus.Disconnected"/>.
        /// </summary>
        Action Disconnected { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Blocked"/>.
        /// </summary>
        Action Blocked { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Unblocked"/>.
        /// </summary>
        Action Unblocked { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.MessageReturned"/>.
        /// </summary>
        Action<byte[], MessageProperties, MessageReturnedInfo> MessageReturned { get; }
    }
}