namespace EasyNetQ
{
    public interface IMessageConsumer
    {
        void Consume<TMessage, TConsumer>(TMessage message) where TMessage : class;
    }
}