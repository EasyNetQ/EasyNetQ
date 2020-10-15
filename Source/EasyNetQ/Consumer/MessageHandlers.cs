using System.Threading;
using System.Threading.Tasks;

namespace EasyNetQ.Consumer
{
    /// <summary>
    ///     Represents a delegate which is called by consumer for every message
    /// </summary>
    public delegate Task<AckStrategy> MessageHandler(
        byte[] body,
        MessageProperties properties,
        MessageReceivedInfo receivedInfo,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Represents a delegate which is called by consumer for every message
    /// </summary>
    public delegate Task<AckStrategy> IMessageHandler(
        IMessage message,
        MessageReceivedInfo receivedInfo,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Represents a delegate which is called by consumer for every message
    /// </summary>
    public delegate Task<AckStrategy> IMessageHandler<in T>(
        IMessage<T> message,
        MessageReceivedInfo receivedInfo,
        CancellationToken cancellationToken
    );
}
