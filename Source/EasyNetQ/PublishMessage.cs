namespace EasyNetQ;

/// <summary>
///     Represents a publishing message
/// </summary>
public readonly struct PublishMessage
{
    /// <summary>
    ///    Creates ProducedMessage
    /// </summary>
    /// <param name="properties">The properties</param>
    /// <param name="body">The body</param>
    public PublishMessage(in MessageProperties properties, in ReadOnlyMemory<byte> body)
    {
        Properties = properties;
        Body = body;
    }

    /// <summary>
    ///     Various message properties
    /// </summary>
    public MessageProperties Properties { get; }

    /// <summary>
    ///     Message body
    /// </summary>
    public ReadOnlyMemory<byte> Body { get; }
}
