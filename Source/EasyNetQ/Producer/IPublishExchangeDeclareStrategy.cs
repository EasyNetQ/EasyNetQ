using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public interface IPublishExchangeDeclareStrategy
    {
        IExchange DeclareExchange(string exchangeName, string exchangeType);
        IExchange DeclareExchange(Type messageType, string exchangeType);        
        Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType);
        Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType);
    }
}