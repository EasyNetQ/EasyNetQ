using EasyNetQ.Topology;

namespace EasyNetQ.Producer;

public interface IExchangeDeclareStrategy
{
    Task<Exchange> DeclareExchangeAsync(string exchangeName, string exchangeType, CancellationToken cancellationToken = default);
    Task<Exchange> DeclareExchangeAsync(Type messageType, string exchangeType, CancellationToken cancellationToken = default);
}
