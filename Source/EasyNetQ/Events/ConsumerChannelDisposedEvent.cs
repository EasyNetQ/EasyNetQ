namespace EasyNetQ.Events;

public readonly record struct ConsumerChannelDisposedEvent(IReadOnlyCollection<string> ConsumerTags);
