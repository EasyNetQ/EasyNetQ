namespace EasyNetQ.Producer
{
    /// <summary>
    /// Creates the appropriate <see cref="IPublisher"/> for an <see cref="IConnectionConfiguration"/>
    /// </summary>
    public static class PublisherFactory
    {
        public static IPublisher CreatePublisher(IConnectionConfiguration configuration, IEasyNetQLogger logger, IEventBus eventBus)
        {
            return configuration.PublisherConfirms
                       ? (IPublisher) new PublisherConfirms(configuration, logger, eventBus)
                       : new PublisherBasic(eventBus);
        }
    }
}