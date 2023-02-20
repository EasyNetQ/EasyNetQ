namespace EasyNetQ.Interception;

public class CompositePublishConsumerInterceptor : IPublishConsumeInterceptor
{
    private readonly IPublishConsumeInterceptor[] interceptors;

    public CompositePublishConsumerInterceptor(IEnumerable<IPublishConsumeInterceptor> interceptors)
    {
        this.interceptors = interceptors.ToArray();
    }

    public PublishMessage OnPublish(in PublishMessage message) => interceptors.OnPublish(message);

    public ConsumeMessage OnConsume(in ConsumeMessage message) => interceptors.OnConsume(message);
}
