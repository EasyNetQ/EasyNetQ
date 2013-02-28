namespace EasyNetQ
{
    public interface IMessageDispatcher
    {
        void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>;
    }
}