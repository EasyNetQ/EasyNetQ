using System;

namespace EasyNetQ
{
    /// <summary>
    /// Represents a handler container for events available in <see cref="IAdvancedBus"/>.
    /// </summary>
    public class AdvancedBusEventHandlers
    {
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Connected"/>.
        /// </summary>
        public EventHandler<ConnectedEventArgs> Connected { get; }

        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Disconnected"/>.
        /// </summary>
        public EventHandler<DisconnectedEventArgs> Disconnected { get; }

        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Blocked"/>.
        /// </summary>
        public EventHandler<BlockedEventArgs> Blocked { get; }

        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Unblocked"/>.
        /// </summary>
        public EventHandler Unblocked { get; }

        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.MessageReturned"/>.
        /// </summary>
        public EventHandler<MessageReturnedEventArgs> MessageReturned { get; }

        /// <summary>
        ///     Creates AdvancedBusEventHandlers
        /// </summary>
        /// <param name="connected">The connected event handler</param>
        /// <param name="disconnected">The disconnected event handler</param>
        /// <param name="blocked">The blocked event handler</param>
        /// <param name="unblocked">The unblocked event handler</param>
        /// <param name="messageReturned">The message returned event handler</param>
        public AdvancedBusEventHandlers(
            EventHandler<ConnectedEventArgs> connected = null,
            EventHandler<DisconnectedEventArgs> disconnected = null,
            EventHandler<BlockedEventArgs> blocked = null,
            EventHandler unblocked = null,
            EventHandler<MessageReturnedEventArgs> messageReturned = null
        )
        {
            // It's ok for any of the specified handler to be null.
            // This allows the caller to specify only the events that he wants to handle when RabbitAdvancedBus is instantiated.

            Connected = connected;
            Disconnected = disconnected;
            Blocked = blocked;
            Unblocked = unblocked;
            MessageReturned = messageReturned;
        }
    }
}
