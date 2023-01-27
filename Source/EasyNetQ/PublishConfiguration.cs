namespace EasyNetQ;

/// <summary>
/// Allows publish configuration to be fluently extended without adding overloads
///
/// e.g.
/// x => x.WithTopic("*.brighton").WithPriority(2)
/// </summary>
public interface IPublishConfiguration
{
    /// <summary>
    /// Sets a priority of the message
    /// </summary>
    /// <param name="priority">The priority to set</param>
    /// <returns>Returns a reference to itself</returns>
    IPublishConfiguration WithPriority(byte priority);

    /// <summary>
    /// Sets a topic for the message
    /// </summary>
    /// <param name="topic">The topic to set</param>
    /// <returns>Returns a reference to itself</returns>
    IPublishConfiguration WithTopic(string topic);

    /// <summary>
    /// Sets a TTL for the message
    /// </summary>
    /// <param name="expires">The TTL to set in milliseconds</param>
    /// <returns>Returns a reference to itself</returns>
    IPublishConfiguration WithExpires(TimeSpan expires);

    /// <summary>
    /// Sets headers
    /// </summary>
    /// <param name="headers">Headers to set</param>
    /// <returns>Returns a reference to itself</returns>
    IPublishConfiguration WithHeaders(IDictionary<string, object?> headers);

    /// <summary>
    /// Allows to propagate trace context information that enables distributed tracing scenarios.
    /// <seealso href="https://www.w3.org/TR/trace-context/" />
    /// <seealso href="https://opentelemetry.io" />
    /// </summary>
    /// <returns><see cref="IPublishConfiguration"/></returns>
    IPublishConfiguration WithTraceContext(bool propagateTraceContext = true);
}

internal class PublishConfiguration : IPublishConfiguration
{
    public PublishConfiguration(string defaultTopic)
    {
        Topic = defaultTopic;
    }

    public IPublishConfiguration WithPriority(byte priority)
    {
        Priority = priority;
        return this;
    }

    public IPublishConfiguration WithTopic(string topic)
    {
        Topic = topic;
        return this;
    }

    public IPublishConfiguration WithExpires(TimeSpan expires)
    {
        Expires = expires;
        return this;
    }

    public IPublishConfiguration WithHeaders(IDictionary<string, object?> headers)
    {
        Headers = headers;
        return this;
    }

    public IPublishConfiguration WithTraceContext(bool propagateTraceContext = true)
    {
        PropagateTraceContext = propagateTraceContext;
        return this;
    }

    public byte? Priority { get; private set; }
    public string Topic { get; private set; }
    public TimeSpan? Expires { get; private set; }
    public IDictionary<string, object?>? Headers { get; private set; }
    public bool PropagateTraceContext { get; private set; }
}
