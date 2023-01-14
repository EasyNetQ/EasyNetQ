namespace EasyNetQ.Events;

public readonly struct DeliveredMessageEvent
{
    public MessageReceivedInfo ReceivedInfo { get; }
    public MessageProperties Properties { get; }
    public ReadOnlyMemory<byte> Body { get; }

    public DeliveredMessageEvent(in MessageReceivedInfo info, in MessageProperties properties, in ReadOnlyMemory<byte> body)
    {
        ReceivedInfo = info;
        Properties = properties;
        Body = body;
    }
}
