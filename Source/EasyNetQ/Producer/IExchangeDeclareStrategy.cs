using System;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    public interface IExchangeDeclareStrategy
    {
        Task<IExchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken = default);
        Task<IExchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken = default);
    }
}