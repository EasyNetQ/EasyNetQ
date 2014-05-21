using System;
using EasyNetQ.FluentConfiguration;

namespace EasyNetQ.AutoSubscribe
{
    public interface IConsume<in T> where T : class
    {
        void Consume(T message);
    }

    
}