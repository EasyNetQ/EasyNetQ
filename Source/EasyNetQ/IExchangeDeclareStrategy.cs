using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ;

public interface IExchangeDeclareStrategy
{
    Task<Exchange> DeclareExchangeAsync(
        string exchangeName,
        string exchangeType,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    );
    Task<Exchange> DeclareExchangeAsync(
        Type messageType,
        string exchangeType,
        TimeBudget timeout,
        CancellationToken cancellationToken = default
    );
}
