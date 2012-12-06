using System;

namespace EasyNetQ
{
    public class DefaultMessageConsumer : IMessageConsumer
    {
        public void Consume<TMessage, TConsumer>(TMessage message) where TMessage : class
        {
            var consumer = (IConsume<TMessage>)Activator.CreateInstance(typeof(TConsumer));

            consumer.Consume(message);
        }
    }
}