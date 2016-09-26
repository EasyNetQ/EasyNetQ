using System;
using System.Collections.Generic;
using System.Text;

namespace EasyNetQ
{
    public class PersistentConnectionFactory : IPersistentConnectionFactory
    {
        private readonly IEventBus eventBus;
        private readonly IConnectionFactory connectionFactory;
        private readonly IEasyNetQLogger logger;

        public PersistentConnectionFactory(IEasyNetQLogger logger, IConnectionFactory connectionFactory, IEventBus eventBus)
        {
            this.logger = logger;
            this.connectionFactory = connectionFactory;
            this.eventBus = eventBus;
        }

        public IPersistentConnection CreateConnection()
        {
            return new PersistentConnection(connectionFactory, logger, eventBus);
        }
    }
}
