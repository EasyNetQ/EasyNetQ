namespace EasyNetQ.Producer
{
    public interface IClientCommandDispatcherFactory
    {
        IClientCommandDispatcher GetClientCommandDispatcher(IPersistentConnection connection);
    }

    public class ClientCommandDispatcherFactory : IClientCommandDispatcherFactory
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IPersistentChannelFactory persistentChannelFactory;

        public ClientCommandDispatcherFactory(ConnectionConfiguration configuration, IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");
            this.configuration = configuration;
            this.persistentChannelFactory = persistentChannelFactory;
        }

        public IClientCommandDispatcher GetClientCommandDispatcher(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");
            return new ClientCommandDispatcher(configuration, connection, persistentChannelFactory);
        }
    }
}