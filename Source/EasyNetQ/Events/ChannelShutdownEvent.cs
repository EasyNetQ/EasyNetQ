using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event which is raised after a shutdown of the channel
/// </summary>
/// <param name="Channel">The affected channel</param>
public readonly record struct ChannelShutdownEvent(IModel Channel);
