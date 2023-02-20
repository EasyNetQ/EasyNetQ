namespace EasyNetQ.Interception;

/// <summary>
///     Allows to execute arbitrary code before publish or consume
/// </summary>
public interface IPublishConsumeInterceptor
{
    /// <summary>
    ///     Allows to execute arbitrary code before publish
    /// </summary>
    /// <param name="message">The source message</param>
    /// <returns>The result message</returns>
    PublishMessage OnPublish(in PublishMessage message);

    /// <summary>
    ///     Allows to execute arbitrary code before consume
    /// </summary>
    /// <param name="message">The source message</param>
    /// <returns>The result message</returns>
    ConsumeMessage OnConsume(in ConsumeMessage message);
}
