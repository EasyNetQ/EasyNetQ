using System;

namespace EasyNetQ.Events
{
    public class ConnectionCreatedEvent
    {
        public readonly Guid Identifier;

        public ConnectionCreatedEvent(Guid identifier)
        {
            Identifier = identifier;
        }
    }
}