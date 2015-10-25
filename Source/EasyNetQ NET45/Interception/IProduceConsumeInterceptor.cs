namespace EasyNetQ.Interception
{
    public interface IProduceConsumeInterceptor
    {
        RawMessage OnProduce(RawMessage rawMessage);
        RawMessage OnConsume(RawMessage rawMessage);
    }
}