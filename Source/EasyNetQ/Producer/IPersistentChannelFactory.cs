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

        public PersistentChannelFactory(IEasyNetQLogger logger, IConnectionConfiguration configuration)
        {
            Preconditions.CheckNotNull(logger, "logger");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.logger = logger;
            this.configuration = configuration;
        }

        public IPersistentChannel CreatePersistentChannel(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(connection, logger, configuration);
        }
    }
}