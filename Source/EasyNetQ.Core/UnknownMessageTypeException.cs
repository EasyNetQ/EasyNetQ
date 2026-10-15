namespace EasyNetQ;

/// <summary>
///     A consumed message's wire type name resolves to no type this consumer handles, or the message carries no
///     type and the consumer has no default. Retrying cannot fix it; the error strategy decides what happens.
/// </summary>
public sealed class UnknownMessageTypeException : EasyNetQException
{
    /// <summary>
    ///     Creates the exception
    /// </summary>
    public UnknownMessageTypeException(string? wireName, string message) : base(message) => WireName = wireName;

    /// <summary>The incoming wire type name; null when the message had no type property</summary>
    public string? WireName { get; }
}
