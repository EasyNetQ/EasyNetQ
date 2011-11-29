using System.Collections.Generic;

namespace EasyNetQ.Hosepipe
{
    public interface IQueueInsertion {
        void PublishMessagesToQueue(IEnumerable<string> messages, QueueParameters parameters);
    }
}