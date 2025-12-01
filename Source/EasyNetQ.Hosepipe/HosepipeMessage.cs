namespace EasyNetQ.Hosepipe;

public class HosepipeMessage
{
    public string Body { get; }
    public MessageProperties Properties { get; }
    public MessageReceivedInfo Info { get; }

    public HosepipeMessage(string body, in MessageProperties properties, in MessageReceivedInfo info)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Properties = properties;
        Info = info;
    }
}
