namespace EasyNetQ.Interception;

internal static class ProduceConsumerInterceptorExtensions
{
    public static ProducedMessage OnProduce(this IProduceConsumeInterceptor[] interceptors, in ProducedMessage message)
    {
        var result = message;
        // ReSharper disable once LoopCanBeConvertedToQuery
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < interceptors.Length; index++)
            result = interceptors[index].OnProduce(result);
        return result;
    }

    public static ConsumedMessage OnConsume(this IProduceConsumeInterceptor[] interceptors, in ConsumedMessage message)
    {
        var result = message;
        for (var index = interceptors.Length - 1; index >= 0; index--)
            result = interceptors[index].OnConsume(result);
        return result;
    }
}
