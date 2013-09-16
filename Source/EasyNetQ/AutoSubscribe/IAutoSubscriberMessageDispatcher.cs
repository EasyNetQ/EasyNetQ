namespace EasyNetQ.AutoSubscribe
{
    public interface IAutoSubscriberMessageDispatcher
    {
        void Dispatch<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsume<TMessage>;
    }
}