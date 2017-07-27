using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    public class PublishChannelCreatedEvent
    {
        public IModel Channel { get; }

        public PublishChannelCreatedEvent(IModel channel)
        {
            Channel = channel;
        }
    }
}   