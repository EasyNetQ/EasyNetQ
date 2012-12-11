using System;

namespace EasyNetQ
{
    public class DefaultMessageDispatcher : IMessageDispatcher
    {
        public void Dispatch<TMessage, TConsumer>(TMessage message) where TMessage : class
        {
            var consumer = (IConsume<TMessage>)Activator.CreateInstance(typeof(TConsumer));

            consumer.Consume(message);
        }
    }
}