using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    public class PublishChannelCreatedEvent
    {
        public IModel Channel { get; private set; }

        public PublishChannelCreatedEvent(IModel channel)
        {
            Channel = channel;
        }
    }
}   