using RabbitMQ.Client;

namespace EasyNetQ.Events;

/// <summary>
///     This event is raised after a successful recovery of the channel
/// </summary>
/// <param name="Channel">The affected channel</param>
public readonly record struct ChannelRecoveredEvent(IModel Channel);
