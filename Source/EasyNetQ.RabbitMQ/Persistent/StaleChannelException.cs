using RabbitMQ.Client.Events;

namespace EasyNetQ.Persistent;

/// <summary>
///     Thrown by channel actions that find the channel already closed before doing any work. Unlike a failure of
///     the action itself, a pre-existing close is never the current operation's fault, so
///     <see cref="PersistentChannel" /> reacts by recreating the channel and retrying the action.
/// </summary>
internal sealed class StaleChannelException : Exception
{
    public StaleChannelException(ShutdownEventArgs closeReason)
        : base($"Channel was already closed: {closeReason}")
    {
    }
}
