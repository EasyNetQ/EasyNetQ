namespace EasyNetQ;

/// <summary>
/// Allows publish configuration to be fluently extended without adding overloads.
/// </summary>
public readonly record struct PublishConfiguration
{
    /// <summary>
    /// Priority of the message
    /// </summary>
    public byte? Priority { get; init; }

    /// <summary>
    /// Topic for the message
    /// </summary>
    public string? Topic { get; init; }

    /// <summary>
    /// A TTL for the message
    /// </summary>
    public TimeSpan? Expires { get; init; }

    /// <summary>
    /// Headers for the message
    /// </summary>
    public IDictionary<string, object?>? Headers { get; init; }
}
