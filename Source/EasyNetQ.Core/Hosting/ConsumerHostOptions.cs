namespace EasyNetQ.Hosting;

/// <summary>
///     How the consumer host starts the fluent-registered consumers. By default they start in the background and
///     retry until they run, so a broker outage or an exchange another app has not declared yet neither blocks nor
///     crashes host startup; <see cref="IConsumerHostStatus" /> reports when they are running.
/// </summary>
public sealed class ConsumerHostOptions
{
    /// <summary>
    ///     Make host startup wait until every consumer runs (retrying as in the background mode, bounded by the
    ///     host's startup cancellation). Default false.
    /// </summary>
    public bool WaitForStartup { get; set; }

    /// <summary>First retry delay after a failed start; doubles per attempt up to <see cref="MaxRetryDelay" /></summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Upper bound of the retry delay</summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
