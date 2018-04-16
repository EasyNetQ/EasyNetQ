using EasyNetQ.Logging;

namespace EasyNetQ.Producer
{
    public interface IPersistentChannelFactory
    {
        IPersistentChannel CreatePersistentChannel(IPersistentConnection connection);
    }

    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IEventBus eventBus;
        
        public PersistentChannelFactory(ConnectionConfiguration configuration, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.configuration = configuration;
            this.eventBus = eventBus;
        }

        public IPersistentChannel CreatePersistentChannel(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(connection, configuration, eventBus);
        }
    }
}