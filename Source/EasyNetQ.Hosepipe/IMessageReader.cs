namespace EasyNetQ.Hosepipe;

public interface IMessageReader
{
    IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, CancellationToken cancellationToken = default);
    IAsyncEnumerable<HosepipeMessage> ReadMessagesAsync(QueueParameters parameters, string messageName, CancellationToken cancellationToken = default);
}
