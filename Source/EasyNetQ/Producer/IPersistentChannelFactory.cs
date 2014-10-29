namespace EasyNetQ.Producer
{
    public interface IPersistentChannelFactory
    {
        IPersistentChannel CreatePersistentChannel(IPersistentConnection connection);
    }

    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly IEasyNetQLogger logger;
        private readonly ConnectionConfiguration configuration;
        private readonly IEventBus eventBus;

        public PersistentChannelFactory(IEasyNetQLogger logger, ConnectionConfiguration configuration, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.logger = logger;
            this.configuration = configuration;
            this.eventBus = eventBus;
        }

        public IPersistentChannel CreatePersistentChannel(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(connection, logger, configuration, eventBus);
        }
    }
}