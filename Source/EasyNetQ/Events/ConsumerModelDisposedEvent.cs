namespace EasyNetQ.Events;

public readonly record struct ConsumerModelDisposedEvent(IReadOnlyCollection<string> ConsumerTags);
