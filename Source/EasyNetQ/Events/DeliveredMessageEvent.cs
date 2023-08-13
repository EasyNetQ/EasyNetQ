namespace EasyNetQ.Events;

public readonly record struct DeliveredMessageEvent(in MessageReceivedInfo Info, in MessageProperties Properties, in ReadOnlyMemory<byte> Body);
