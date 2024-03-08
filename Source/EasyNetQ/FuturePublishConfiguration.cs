namespace EasyNetQ;

/// <summary>
///     Allows future publish configuration to be fluently extended without adding overloads
///     e.g.
///     x => x.WithTopic("*.brighton").WithPriority(2)
/// </summary>
public interface IFuturePublishConfiguration
{
    /// <summary>
    ///     Sets a priority of the message
    /// </summary>
    /// <param name="priority">The priority to set</param>
    /// <returns>Returns a reference to itself</returns>
    IFuturePublishConfiguration WithPriority(byte priority);

    /// <summary>
    ///     Sets a topic for the message
    /// </summary>
    /// <param name="topic">The topic to set</param>
    /// <returns>Returns a reference to itself</returns>
    IFuturePublishConfiguration WithTopic(string topic);

    /// <summary>
    /// Sets headers
    /// </summary>
    /// <param name="headers">Headers to set</param>
    /// <returns>Returns a reference to itself</returns>
    IFuturePublishConfiguration WithHeaders(IDictionary<string, object?> headers);

    /// <summary>
    /// Set publisher confirms
    /// </summary>
    /// <param name="publisherConfirms">Publisher confirms flag to set</param>
    /// <returns>Returns a reference to itself</returns>
    IFuturePublishConfiguration WithPublisherConfirms(bool publisherConfirms);
}

internal class FuturePublishConfiguration : IFuturePublishConfiguration
{
    public FuturePublishConfiguration(string defaultTopic)
    {
        Topic = defaultTopic;
    }

    public byte? Priority { get; private set; }
    public string Topic { get; private set; }
    public IDictionary<string, object?>? MessageHeaders { get; private set; }
    public bool PublisherConfirms { get; private set; }

    public IFuturePublishConfiguration WithPriority(byte priority)
    {
        Priority = priority;
        return this;
    }

    public IFuturePublishConfiguration WithTopic(string topic)
    {
        Topic = topic;
        return this;
    }

    public IFuturePublishConfiguration WithHeaders(IDictionary<string, object?> headers)
    {
        foreach (var kvp in headers)
            (MessageHeaders ??= new Dictionary<string, object?>()).Add(kvp.Key, kvp.Value);
        return this;
    }

    public IFuturePublishConfiguration WithPublisherConfirms(bool publisherConfirms)
    {
        PublisherConfirms = publisherConfirms;
        return this;
    }
}
