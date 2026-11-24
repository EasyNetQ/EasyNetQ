using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Persistent;

internal static class ConnectionExtensions
{
    public static void EnsureIsOpen(this IConnection connection)
    {
        var closeReason = connection.CloseReason;
        if (closeReason is null) return;

        throw new AlreadyClosedException(closeReason);
    }
}
