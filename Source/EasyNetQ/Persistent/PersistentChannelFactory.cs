namespace EasyNetQ.Persistent
{
    /// <inheritdoc />
    public class PersistentChannelFactory : IPersistentChannelFactory
    {
        private readonly IEventBus eventBus;

        /// <summary>
        ///    Creates PersistentChannelFactory
        /// </summary>
        public PersistentChannelFactory(IEventBus eventBus)
        {
            Preconditions.CheckNotNull(eventBus, nameof(eventBus));

            this.eventBus = eventBus;
        }

        /// <inheritdoc />
        public IPersistentChannel CreatePersistentChannel(
            IPersistentConnection connection, PersistentChannelOptions options
        )
        {
            return new PersistentChannel(options, connection, eventBus);
        }
    }
}
