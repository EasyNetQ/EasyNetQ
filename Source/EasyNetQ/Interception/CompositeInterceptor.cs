using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception;

internal sealed class CompositeInterceptor : IProduceConsumeInterceptor
{
    private readonly List<IProduceConsumeInterceptor> interceptors = new();

    public CompositeInterceptor() { }

    public CompositeInterceptor(IEnumerable<IProduceConsumeInterceptor> interceptors)
    {
        this.interceptors.AddRange(interceptors);
    }

    /// <inheritdoc />
    public ProducedMessage OnProduce(in ProducedMessage message)
    {
        return interceptors.AsEnumerable()
            .Aggregate(message, (x, y) => y.OnProduce(x));
    }

    /// <inheritdoc />
    public ConsumedMessage OnConsume(in ConsumedMessage message)
    {
        return interceptors.AsEnumerable()
            .Reverse()
            .Aggregate(message, (x, y) => y.OnConsume(x));
    }

    /// <summary>
    ///     Add the interceptor to pipeline
    /// </summary>
    /// <param name="interceptor"></param>
    public void Add(IProduceConsumeInterceptor interceptor) // TODO: method may be removed, for test purposes only
    {
        interceptors.Add(interceptor);
    }
}
