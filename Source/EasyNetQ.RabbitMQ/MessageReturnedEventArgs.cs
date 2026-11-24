namespace EasyNetQ;

public class MessageReturnedEventArgs : EventArgs
{
    public ReadOnlyMemory<byte> MessageBody { get; }
    public MessageProperties MessageProperties { get; }
    public MessageReturnedInfo MessageReturnedInfo { get; }

    public MessageReturnedEventArgs(in ReadOnlyMemory<byte> messageBody, in MessageProperties messageProperties, in MessageReturnedInfo messageReturnedInfo)
    {
        MessageBody = messageBody;
        MessageProperties = messageProperties;
        MessageReturnedInfo = messageReturnedInfo;
    }
}
