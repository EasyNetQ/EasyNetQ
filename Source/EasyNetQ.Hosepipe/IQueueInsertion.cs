namespace EasyNetQ.Hosepipe;

public interface IQueueInsertion
{
    Task PublishMessagesToQueueAsync(IAsyncEnumerable<HosepipeMessage> messages, QueueParameters parameters, CancellationToken cancellationToken = default);
}
