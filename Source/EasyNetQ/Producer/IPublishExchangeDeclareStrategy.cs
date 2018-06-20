using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public interface IPublishExchangeDeclareStrategy
    {
        Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType);
        Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType);
    }
}