namespace EasyNetQ
{
    public interface IMessageConsumer
    {
        void Consume<TMessage, TMessageHandler>(TMessage message) where TMessage : class;
    }
}