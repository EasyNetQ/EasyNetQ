namespace EasyNetQ.Hosepipe;

public interface IMessageReader
{
    IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters);
    IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, string messageName);
}
