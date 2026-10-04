using System.Buffers;

namespace EasyNetQ;

/// <summary>
///     The descriptor of messages dispatched to <see cref="HandlerTable.HandleUnknown" />: the body stays raw bytes.
///     Only the consume path uses it.
/// </summary>
internal sealed class RawMessageTypeDescriptor : MessageTypeDescriptor
{
    public static readonly RawMessageTypeDescriptor Instance = new();

    private RawMessageTypeDescriptor() : base(typeof(ReadOnlyMemory<byte>), "")
    {
    }

    public override IMemoryOwner<byte> SerializeBody(IMessageSerializer serializer, object body)
        => throw new NotSupportedException("Unknown messages are consume-only");

    public override object? DeserializeBody(IMessageSerializer serializer, in ReadOnlyMemory<byte> body) => body;

    public override IMessage CreateMessage(object? body, in MessageProperties properties)
        => new Message<ReadOnlyMemory<byte>>(body is ReadOnlyMemory<byte> bytes ? bytes : default, properties);

    internal override Task PublishViaAsync(IPubSub pubSub, object message, Action<IPublishConfiguration> configure, CancellationToken cancellationToken)
        => throw new NotSupportedException("Unknown messages are consume-only");

    internal override Task<SubscriptionResult> SubscribeViaAsync(IPubSub pubSub, string subscriptionId, Func<object, Type, CancellationToken, Task> onMessage, Action<ISubscriptionConfiguration> configure, CancellationToken cancellationToken)
        => throw new NotSupportedException("Unknown messages are consume-only");

    internal override Task SendViaAsync(ISendReceive sendReceive, string queue, object message, Action<ISendConfiguration> configure, CancellationToken cancellationToken)
        => throw new NotSupportedException("Unknown messages are consume-only");

    internal override Task FuturePublishViaAsync(IScheduler scheduler, object message, TimeSpan delay, Action<IFuturePublishConfiguration> configure, CancellationToken cancellationToken)
        => throw new NotSupportedException("Unknown messages are consume-only");
}
