namespace EasyNetQ.Interception
{
    /// <summary>
    ///     An empty interceptor
    /// </summary>
    public class DefaultInterceptor : IProduceConsumeInterceptor
    {
        /// <inheritdoc />
        public ProducedMessage OnProduce(ProducedMessage message) => message;

        /// <inheritdoc />
        public ConsumedMessage OnConsume(ConsumedMessage message) => message;
    }
}
