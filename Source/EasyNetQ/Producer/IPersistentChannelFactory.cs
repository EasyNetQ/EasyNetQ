namespace EasyNetQ.Producer
{
    public readonly struct PersistentChannelOptions
    {
        public PersistentChannelOptions(bool publisherConfirms)
        {
            PublisherConfirms = publisherConfirms;
        }

        public bool PublisherConfirms { get; }
    }

    public interface IPersistentChannelFactory
    {
        IPersistentChannel CreatePersistentChannel(IPersistentConnection connection, PersistentChannelOptions options);
    }

    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly ConnectionConfiguration configuration;
        private readonly IEventBus eventBus;

        public PersistentChannelFactory(IEventBus eventBus)
        {
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.eventBus = eventBus;
        }

        public IPersistentChannel CreatePersistentChannel(
            IPersistentConnection connection, PersistentChannelOptions options
        )
        {
            Preconditions.CheckNotNull(connection, "connection");

            return new PersistentChannel(options, connection, eventBus);
        }
    }
}
