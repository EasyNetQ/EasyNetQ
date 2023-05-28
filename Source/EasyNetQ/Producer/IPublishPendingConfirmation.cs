namespace EasyNetQ.Producer;

/// <summary>
/// Pending confirmation which could be waited for ack, nack and etc.
/// </summary>
public interface IPublishPendingConfirmation
{
    /// <summary>
    ///     Identifier of the confirmation
    /// </summary>
    ulong Id { get; }

    /// <summary>
    ///     Wait confirmation for ack, nack and etc.
    /// </summary>
    /// <param name="timeout"></param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns></returns>
    Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Cancel confirmation
    /// </summary>
    void Cancel();
}
