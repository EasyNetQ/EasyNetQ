using System;

namespace EasyNetQ
{
    public interface IMessageValidationStrategy
    {
        void CheckMessageType<TMessage>(
            Byte[] body, 
            MessageProperties properties, 
            MessageReceivedInfo messageReceivedInfo);
    }
}