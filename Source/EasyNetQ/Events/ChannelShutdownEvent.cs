using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    /// <summary>
    ///     This event which is raised after a shutdown of the channel
    /// </summary>
    public class ChannelShutdownEvent
    {
        /// <summary>
        ///     The closed channel
        /// </summary>
        public IModel Channel { get; }

        /// <summary>
        ///     Creates an event
        /// </summary>
        /// <param name="channel">The affected channel</param>
        public ChannelShutdownEvent(IModel channel)
        {
            Channel = channel;
        }
    }
}
