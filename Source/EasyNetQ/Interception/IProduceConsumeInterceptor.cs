namespace EasyNetQ.Interception
{
    /// <summary>
    ///     Allow to execute arbitrary code before publish or consume
    /// </summary>
    public interface IProduceConsumeInterceptor
    {
        /// <summary>
        ///     Allows to execute arbitrary code before publish
        /// </summary>
        /// <param name="message">The source message</param>
        /// <returns>The result message</returns>
        ProducedMessage OnProduce(in ProducedMessage message);

        /// <summary>
        ///     Allows to execute arbitrary code before consume
        /// </summary>
        /// <param name="message">The source message</param>
        /// <returns>The result message</returns>
        ConsumedMessage OnConsume(in ConsumedMessage message);
    }
}
