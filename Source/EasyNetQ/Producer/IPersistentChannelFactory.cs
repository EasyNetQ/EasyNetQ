using EasyNetQ.Producer.Waiters;

namespace EasyNetQ.Producer
{
    public interface IPersistentChannelFactory
    {
        IPersistentChannel CreatePersistentChannel(IPersistentConnection connection);
    }

    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly IEasyNetQLogger logger;
        private readonly IConnectionConfiguration configuration;
        private readonly IEventBus eventBus;
        private readonly IReconnectionWaiterFactory reconnectionWaiterFactory;

        public PersistentChannelFactory(IEasyNetQLogger logger, IConnectionConfiguration configuration, IEventBus eventBus, IReconnectionWaiterFactory reconnectionWaiterFactory)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(reconnectionWaiterFactory, "reconnectionWaiterFactory");

            this.logger = logger;
            this.configuration = configuration;
            this.eventBus = eventBus;
            this.reconnectionWaiterFactory = reconnectionWaiterFactory;
        }

        public IPersistentChannel CreatePersistentChannel(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(connection, logger, configuration, reconnectionWaiterFactory, eventBus);
        }
    }
}