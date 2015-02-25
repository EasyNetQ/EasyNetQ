using System;

namespace EasyNetQ
{
    public class BusEventHandlers : IBusEventHandlers
    {
        public static readonly BusEventHandlers Default = new BusEventHandlers();

        public Action Connected { get; private set; }
        public Action Disconnected { get; private set; }
        public Action Blocked { get; private set; }
        public Action Unblocked { get; private set; }
        public Action<byte[], MessageProperties, MessageReturnedInfo> MessageReturned { get; private set; }

        public BusEventHandlers(
            Action connected = null,
            Action disconnected = null,
            Action blocked = null,
            Action unblocked = null,
            Action<byte[], MessageProperties, MessageReturnedInfo> messageReturned = null)
        {
            // It's ok for any of the specified handler to be null.
            // This allows the caller to specify only the events that he wants to handle when IAdvancedBus / IBus is instantiated.

            Connected = connected;
            Disconnected = disconnected;
            Blocked = blocked;
            Unblocked = unblocked;
            MessageReturned = messageReturned;
        }
    }
}