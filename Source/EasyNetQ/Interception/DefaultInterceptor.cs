namespace EasyNetQ.Interception
{
    /// <summary>
    ///     An empty interceptor
    /// </summary>
    public class DefaultInterceptor : IProduceConsumeInterceptor
    {
        /// <inheritdoc />
        public virtual ProducedMessage OnProduce(in ProducedMessage message) => message;

        /// <inheritdoc />
        public virtual ConsumedMessage OnConsume(in ConsumedMessage message) => message;

        /// <inheritdoc />
        public virtual void OnProduced(in ProducedMessage message) { }

        /// <inheritdoc />
        public virtual void OnConsumed(in ConsumedMessage message) { }
    }
}
