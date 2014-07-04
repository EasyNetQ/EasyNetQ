using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public interface IPublishExchangeDeclareStrategy
    {
        IExchange DeclareExchange(IAdvancedBus advancedBus, string exchangeName, string exchangeType);
        IExchange DeclareExchange(IAdvancedBus advancedBus, Type messageType, string exchangeType);        
        Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, string exchangeName, string exchangeType);
        Task<IExchange> DeclareExchangeAsync(IAdvancedBus advancedBus, Type messageType, string exchangeType);
    }
}