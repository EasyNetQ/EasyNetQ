namespace EasyNetQ.Consumer;

/// <summary>
///     Settings of <see cref="DefaultConsumeErrorStrategy" />: what it logs and how it declares the error queue.
///     One error queue serves every consumer, so its arguments are a bus-wide choice.
/// </summary>
public sealed class ConsumeErrorOptions
{
    /// <summary>
    ///     Log the failed message's body (base64) next to the error. Off by default: the error queue keeps the body,
    ///     and bodies often carry personal data that should not end up in log storage.
    /// </summary>
    public bool LogMessageBody { get; set; }

    /// <summary>
    ///     Arguments for declaring the error queue (e.g. <c>x-queue-type: quorum</c> so it is replicated on a
    ///     cluster). Merged over <see cref="IConventions.ErrorQueueTypeConvention" />; null declares a classic queue.
    /// </summary>
    public IDictionary<string, object>? ErrorQueueArguments { get; set; }

    /// <summary>
    ///     Name of the error queue; null uses <see cref="IConventions.ErrorQueueNamingConvention" />. A broker that
    ///     scopes permissions by name (e.g. <c>^app\.</c>) needs the error queue under the app's prefix.
    /// </summary>
    public string? QueueName { get; set; }

    /// <summary>
    ///     Name of the error exchange; null uses <see cref="IConventions.ErrorExchangeNamingConvention" />
    /// </summary>
    public string? ExchangeName { get; set; }
}
