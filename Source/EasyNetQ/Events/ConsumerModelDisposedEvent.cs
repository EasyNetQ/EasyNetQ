using System.Collections.Generic;

namespace EasyNetQ.Events
{
    public readonly struct ConsumerModelDisposedEvent
    {
        public IReadOnlyCollection<string> ConsumerTags { get; }

        public ConsumerModelDisposedEvent(string[] consumerTags)
        {
            ConsumerTags = consumerTags;
        }
    }
}
