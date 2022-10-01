using System.Collections.Generic;

namespace EasyNetQ;

/// <summary>
///     Allows send configuration to be fluently extended without adding overloads
///     e.g.
///     x => x.WithPriority(2)
/// </summary>
public interface ISendConfiguration
{
    /// <summary>
    ///     Sets a priority of the message
    /// </summary>
    /// <param name="priority">The priority to set</param>
    /// <returns>Returns a reference to itself</returns>
    ISendConfiguration WithPriority(byte priority);

    /// <summary>
    /// Sets headers
    /// </summary>
    /// <param name="headers">Headers to set</param>
    /// <returns>Returns a reference to itself</returns>
    ISendConfiguration WithHeaders(IDictionary<string, object?> headers);
}

internal class SendConfiguration : ISendConfiguration
{
    public byte? Priority { get; private set; }
    public IDictionary<string, object?>? Headers { get; private set; }

    public ISendConfiguration WithPriority(byte priority)
    {
        Priority = priority;
        return this;
    }

    public ISendConfiguration WithHeaders(IDictionary<string, object?> headers)
    {
        Headers = headers;
        return this;
    }
}
