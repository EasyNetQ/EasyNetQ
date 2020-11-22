using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.IntegrationTests.Utils
{
    public class MessagesSink
    {
        private readonly TaskCompletionSource<object> allMessagedReceived = new TaskCompletionSource<object>();
        private readonly object locker = new object();
        private readonly int maxCount;
        private readonly List<Message> receivedMessages = new List<Message>();

        public MessagesSink(int maxCount)
        {
            this.maxCount = maxCount;
            if (maxCount == 0)
                allMessagedReceived.TrySetResult(null);
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
            await using (
                cancellationToken.Register(
                    x => ((TaskCompletionSource<object>) x)?.TrySetCanceled(),
                    allMessagedReceived,
                    false
                )
            )
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
                    allMessagedReceived.TrySetResult(null);
            }
        }
    }
}
