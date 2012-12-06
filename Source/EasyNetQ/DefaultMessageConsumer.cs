using System;

namespace EasyNetQ
{
    public class DefaultMessageConsumer : IMessageConsumer
    {
        public void Consume<TMessage, TMessageHandler>(TMessage message) where TMessage : class
        {
            var consumer = (IConsume<TMessage>) Activator.CreateInstance(typeof (TMessageHandler));

            consumer.Consume(message);
        }
    }
}