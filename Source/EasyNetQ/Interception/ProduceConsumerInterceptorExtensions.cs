namespace EasyNetQ.Interception;

internal static class ProduceConsumerInterceptorExtensions
{
    public static PublishMessage OnPublish(this IPublishConsumeInterceptor[] interceptors, in PublishMessage message)
    {
        var result = message;
        // ReSharper disable once LoopCanBeConvertedToQuery
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < interceptors.Length; index++)
            result = interceptors[index].OnPublish(result);
        return result;
    }

    public static ConsumeMessage OnConsume(this IPublishConsumeInterceptor[] interceptors, in ConsumeMessage message)
    {
        var result = message;
        for (var index = interceptors.Length - 1; index >= 0; index--)
            result = interceptors[index].OnConsume(result);
        return result;
    }
}
