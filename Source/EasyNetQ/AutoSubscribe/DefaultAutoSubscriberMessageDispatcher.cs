using System;
using System.Threading.Tasks;

namespace EasyNetQ.AutoSubscribe
{
    public class DefaultAutoSubscriberMessageDispatcher : IAutoSubscriberMessageDispatcher
    {
        public void Dispatch<TMessage, TConsumer>(TMessage message) 
            where TMessage : class
            where TConsumer : IConsume<TMessage>
        {
            var consumer = (IConsume<TMessage>)Activator.CreateInstance(typeof(TConsumer));

            consumer.Consume(message);
        }

        public Task DispatchAsync<TMessage, TConsumer>(TMessage message)
            where TMessage : class
            where TConsumer : IConsumeAsync<TMessage>
        {
            var consumer = (IConsumeAsync<TMessage>)Activator.CreateInstance(typeof(TConsumer));

            return consumer.Consume(message);
        }
    }
}