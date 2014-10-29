namespace EasyNetQ.Producer
{
    /// <summary>
    /// Creates the appropriate <see cref="IPublisher"/> for an <see cref="ConnectionConfiguration"/>
    /// </summary>
    public static class PublisherFactory
    {
        public static IPublisher CreatePublisher(ConnectionConfiguration configuration, IEasyNetQLogger logger, IEventBus eventBus)
        {
            return configuration.PublisherConfirms
                       ? (IPublisher) new PublisherConfirms(configuration, logger, eventBus)
                       : new PublisherBasic(eventBus);
        }
    }
}