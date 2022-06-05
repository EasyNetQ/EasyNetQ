using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Interception;

public class CompositeProduceConsumerInterceptor : IProduceConsumeInterceptor
{
    private readonly IProduceConsumeInterceptor[] interceptors;

    public CompositeProduceConsumerInterceptor(IEnumerable<IProduceConsumeInterceptor> interceptors)
    {
        this.interceptors = interceptors.ToArray();
    }

    public ProducedMessage OnProduce(in ProducedMessage message)
    {
        var result = message;
        // ReSharper disable once LoopCanBeConvertedToQuery
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < interceptors.Length; index++)
            result = interceptors[index].OnProduce(result);
        return result;
    }

    public ConsumedMessage OnConsume(in ConsumedMessage message)
    {
        var result = message;
        for (var index = interceptors.Length - 1; index >= 0; index--)
            result = interceptors[index].OnConsume(result);
        return result;
    }
}
