namespace EasyNetQ.Producer
{
    /// <summary>
    ///     An options for PersistentChannel
    /// </summary>
    public readonly struct PersistentChannelOptions
    {
        /// <summary>
        ///     Creates an options for PersistentChannel
        /// </summary>
        /// <param name="publisherConfirms"></param>
        public PersistentChannelOptions(bool publisherConfirms)
        {
            PublisherConfirms = publisherConfirms;
        }

        /// <summary>
        ///     Enables publisher confirms
        /// </summary>
        public bool PublisherConfirms { get; }
    }

    /// <summary>
    ///     Creates PersistentChannel using the connection and the options
    /// </summary>
    public interface IPersistentChannelFactory
    {
        /// <summary>
        ///     Creates PersistentChannel
        /// </summary>
        /// <param name="options">The channel options</param>
        /// <returns>New PersistentChannel</returns>
        IPersistentChannel CreatePersistentChannel(PersistentChannelOptions options);
    }

    /// <inheritdoc />
    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly IPersistentConnection connection;
        private readonly IEventBus eventBus;

        /// <summary>
        ///    Creates PersistentChannelFactory
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <param name="eventBus">The event bus</param>
        public PersistentChannelFactory(IPersistentConnection connection, IEventBus eventBus)
        {
            Preconditions.CheckNotNull(connection, "connection");
            Preconditions.CheckNotNull(eventBus, "eventBus");

            this.connection = connection;
            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public IPersistentChannel CreatePersistentChannel(PersistentChannelOptions options)
        {
            return new PersistentChannel(options, connection, eventBus);
        }
    }
}
