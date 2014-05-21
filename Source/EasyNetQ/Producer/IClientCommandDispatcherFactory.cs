namespace EasyNetQ.Producer
{
    public interface IClientCommandDispatcherFactory
    {
        IClientCommandDispatcher GetClientCommandDispatcher(IPersistentConnection connection);
    }

    public class ClientCommandDispatcherFactory : IClientCommandDispatcherFactory
    {
        private readonly IPersistentChannelFactory persistentChannelFactory;

        public ClientCommandDispatcherFactory(IPersistentChannelFactory persistentChannelFactory)
        {
            Preconditions.CheckNotNull(persistentChannelFactory, "persistentChannelFactory");
            this.persistentChannelFactory = persistentChannelFactory;
        }

        public IClientCommandDispatcher GetClientCommandDispatcher(IPersistentConnection connection)
        {
            Preconditions.CheckNotNull(connection, "connection");
            return new ClientCommandDispatcher(connection, persistentChannelFactory);
        }
    }
}