namespace EasyNetQ.Interception;

/// <inheritdoc />
public class NoopProduceConsumeInterceptor : IProduceConsumeInterceptor
{
    public ProducedMessage OnProduce(in ProducedMessage message) => message;

    public ConsumedMessage OnConsume(in ConsumedMessage message) => message;
}
