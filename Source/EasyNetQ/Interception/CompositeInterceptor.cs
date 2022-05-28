using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception;

/// <inheritdoc />
public class CompositeInterceptor : IProduceConsumeInterceptor
{
    private readonly IReadOnlyList<IProduceConsumeInterceptor> onProduceInterceptors;
    private readonly IReadOnlyList<IProduceConsumeInterceptor> onConsumeInterceptors;

    public CompositeInterceptor(IReadOnlyList<IProduceConsumeInterceptor> interceptors)
    {
        onProduceInterceptors = interceptors;
        onConsumeInterceptors = interceptors.Reverse().ToList();
    }

    /// <inheritdoc />
    public ProducedMessage OnProduce(in ProducedMessage message)
    {
        return onProduceInterceptors.Aggregate(message, (x, y) => y.OnProduce(x));
    }

    /// <inheritdoc />
    public ConsumedMessage OnConsume(in ConsumedMessage message)
    {
        return onConsumeInterceptors.Aggregate(message, (x, y) => y.OnConsume(x));
    }
}
