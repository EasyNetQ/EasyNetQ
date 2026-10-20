namespace EasyNetQ.IntegrationTests.Utils;

public class MessagesSink
{
    private readonly TaskCompletionSource allMessagedReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly object locker = new();
    private readonly int maxCount;
    private readonly List<Message> receivedMessages = new();

    public MessagesSink(int maxCount)
    {
        this.maxCount = maxCount;
        if (maxCount == 0)
            allMessagedReceived.TrySetResult();
    }

    public IReadOnlyList<Message> ReceivedMessages
    {
        get
        {
            lock (locker)
            {
                return receivedMessages.ToList();
            }
        }
    }

    public async Task WaitAllReceivedAsync(CancellationToken cancellationToken = default)
    {
        await using (cancellationToken.Register(() => allMessagedReceived.TrySetCanceled()))
        {
            await allMessagedReceived.Task;
        }
    }

    public void Receive(Message message)
    {
        lock (locker)
        {
            if (receivedMessages.Count >= maxCount)
                throw new InvalidOperationException();

            receivedMessages.Add(message);
            if (receivedMessages.Count == maxCount)
                allMessagedReceived.TrySetResult();
        }
    }
}
