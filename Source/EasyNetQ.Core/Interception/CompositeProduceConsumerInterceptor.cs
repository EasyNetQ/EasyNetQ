namespace EasyNetQ.Interception;

public class CompositeProduceConsumerInterceptor : IProduceConsumeInterceptor
{
    private readonly IProduceConsumeInterceptor[] interceptors;

    public CompositeProduceConsumerInterceptor(IEnumerable<IProduceConsumeInterceptor> interceptors)
    {
        this.interceptors = interceptors.ToArray();
    }

    public ProducedMessage OnProduce(in ProducedMessage message) => interceptors.OnProduce(message);

    public ConsumedMessage OnConsume(in ConsumedMessage message) => interceptors.OnConsume(message);
}
