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
        ///     Allows to execute arbitrary code after publish
        /// </summary>
        /// <param name="message">The published message</param>
        void OnProduced(in ProducedMessage message);

        /// <summary>
        ///     Allows to execute arbitrary code before consume
        /// </summary>
        /// <param name="message">The source message</param>
        /// <returns>The result message</returns>
        ConsumedMessage OnConsume(in ConsumedMessage message);

        /// <summary>
        ///     Allows to execute arbitrary code after consume
        /// </summary>
        /// <param name="message">The consumed message</param>
        void OnConsumed(in ConsumedMessage message);
    }
}
