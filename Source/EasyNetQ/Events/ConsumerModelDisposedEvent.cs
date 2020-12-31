using System.Collections.Generic;

namespace EasyNetQ.Events
{
    public class ConsumerModelDisposedEvent
    {
        public IReadOnlyCollection<string> ConsumerTags { get; }

        public ConsumerModelDisposedEvent(string[] consumerTags)
        {
            ConsumerTags = consumerTags;
        }
    }
}
