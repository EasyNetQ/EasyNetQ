using System;
using System.Collections.Generic;
using System.Text;

namespace EasyNetQ
{
    public class PersistentConnectionFactory : IPersistentConnectionFactory
    {
        private readonly IEventBus eventBus;
        private readonly IConnectionFactory connectionFactory;

        public PersistentConnectionFactory(IConnectionFactory connectionFactory, IEventBus eventBus)
        {
            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }

        public IPersistentConnection CreateConnection()
        {
            return new PersistentConnection(connectionFactory, eventBus);
        }
    }
}
