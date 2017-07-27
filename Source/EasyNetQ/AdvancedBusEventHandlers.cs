using System;
using RabbitMQ.Client.Events;

namespace EasyNetQ
{
    /// <summary>
    /// Represents a handler container for events available in <see cref="IAdvancedBus"/>.
    /// </summary>
    public class AdvancedBusEventHandlers
    {
        public static readonly AdvancedBusEventHandlers Default = new AdvancedBusEventHandlers();

        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Connected"/>.
        /// </summary>
        public EventHandler Connected { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Disconnected"/>.
        /// </summary>
        public EventHandler Disconnected { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Blocked"/>.
        /// </summary>
        public EventHandler<ConnectionBlockedEventArgs> Blocked { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.Unblocked"/>.
        /// </summary>
        public EventHandler Unblocked { get; }
        /// <summary>
        /// An event handler for <see cref="IAdvancedBus.MessageReturned"/>.
        /// </summary>
        public EventHandler<MessageReturnedEventArgs> MessageReturned { get; }

        public AdvancedBusEventHandlers(
            EventHandler connected = null,
            EventHandler disconnected = null,
            EventHandler<ConnectionBlockedEventArgs> blocked = null,
            EventHandler unblocked = null,
            EventHandler<MessageReturnedEventArgs> messageReturned = null)
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