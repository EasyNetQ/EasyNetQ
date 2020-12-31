namespace EasyNetQ.Interception
{
    /// <summary>
    ///     An empty interceptor
    /// </summary>
    public class DefaultInterceptor : IProduceConsumeInterceptor
    {
        /// <inheritdoc />
        public ProducedMessage OnProduce(in ProducedMessage message) => message;

        /// <inheritdoc />
        public ConsumedMessage OnConsume(in ConsumedMessage message) => message;
    }
}
