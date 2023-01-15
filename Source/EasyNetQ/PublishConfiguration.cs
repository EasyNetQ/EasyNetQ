namespace EasyNetQ;

/// <summary>
/// Allows publish configuration to be fluently extended without adding overloads
///
/// e.g.
/// x => x with { Topic = "*.brighton", Priority = 2 }
/// </summary>
public readonly record struct PublishConfiguration
{
    public PublishConfiguration(string defaultTopic)
    {
        Topic = defaultTopic;
    }

    /// <summary>
    /// Priority of the message
    /// </summary>
    public byte? Priority { get; init; }

    /// <summary>
    /// Topic for the message
    /// </summary>
    public string Topic { get; init; }

    /// <summary>
    /// A TTL for the message
    /// </summary>
    public TimeSpan? Expires { get; init; }

    /// <summary>
    /// Headers for the message
    /// </summary>
    public IDictionary<string, object?>? Headers { get; init; }
}
