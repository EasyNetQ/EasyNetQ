namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a message is published
/// </summary>
/// <param name="Exchange">The exchange</param>
/// <param name="RoutingKey">The routing key</param>
/// <param name="Properties">The properties</param>
/// <param name="Body">The body</param>
public readonly record struct PublishedMessageEvent(string Exchange, string RoutingKey, in MessageProperties Properties, in ReadOnlyMemory<byte> Body);
