namespace EasyNetQ
{
    /// <summary>
    ///     Various extensions for IConsumerConfiguration
    /// </summary>
    public static class ConsumerConfigurationExtensions
    {
        /// <summary>
        /// Sets priority
        /// </summary>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="priority">The priority to set</param>
        /// <returns>IQueueDeclareConfiguration</returns>
        public static IConsumerConfiguration WithPriority(this IConsumerConfiguration configuration, int priority)
        {
            Preconditions.CheckNotNull(configuration, nameof(configuration));
            configuration.WithArgument("x-priority", priority);
            return configuration;
        }
    }
}
