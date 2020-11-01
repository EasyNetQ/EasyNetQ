using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event is raised after a successful recovery of the channel
    /// </summary>
    public class ChannelRecoveredEvent
    {
        /// <summary>
        ///     The recovered channel
        /// </summary>
        public IModel Channel { get; }

        /// <summary>
        ///     Creates an event
        /// </summary>
        /// <param name="channel">The affected channel</param>
        public ChannelRecoveredEvent(IModel channel)
        {
            Channel = channel;
        }
    }
}
