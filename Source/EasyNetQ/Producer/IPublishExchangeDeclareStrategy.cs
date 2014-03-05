using System;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public interface IPublishExchangeDeclareStrategy
    {
        IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType);
        IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType);        
    }
}