namespace EasyNetQ;

public interface IMessageDeliveryModeStrategy
{
    byte GetDeliveryMode(Type messageType);
}

public class MessageDeliveryModeStrategy : IMessageDeliveryModeStrategy
{
    private readonly BusOptions busOptions;
    private readonly IMessageTypeRegistry messageTypeRegistry;

    public MessageDeliveryModeStrategy(BusOptions busOptions, IMessageTypeRegistry messageTypeRegistry)
    {
        this.busOptions = busOptions;
        this.messageTypeRegistry = messageTypeRegistry;
    }

    /// <inheritdoc />
    public byte GetDeliveryMode(Type messageType)
    {
        var isPersistent = messageTypeRegistry.GetOrAdd(messageType).IsPersistent ?? busOptions.PersistentMessages;
        return isPersistent ? MessageDeliveryMode.Persistent : MessageDeliveryMode.NonPersistent;
    }
}
