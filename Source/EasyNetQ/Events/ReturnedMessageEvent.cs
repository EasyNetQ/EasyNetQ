using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a message is returned because it couldn't be routed
/// </summary>
/// <param name="Channel">The channel</param>
/// <param name="Body">The message body</param>
/// <param name="Properties">The message properties</param>
/// <param name="Info">The returned message info</param>
public readonly record struct ReturnedMessageEvent(IModel Channel, in ReadOnlyMemory<byte> Body, in MessageProperties Properties, in MessageReturnedInfo Info);
