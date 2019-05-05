using RabbitMQ.Client;

namespace EasyNetQ.Events
{
    public struct PublishChannelCreatedEvent
    {
        public IModel Channel { get; }

        public PublishChannelCreatedEvent(IModel channel)
        {
            Channel = channel;
        }
    }
}
