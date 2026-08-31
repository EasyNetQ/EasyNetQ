namespace EasyNetQ;

public interface IMessageDeliveryModeStrategy
{
    byte GetDeliveryMode(Type messageType);
}

public class MessageDeliveryModeStrategy : IMessageDeliveryModeStrategy
{
    private readonly ConnectionConfiguration connectionConfiguration;
    private readonly IMessageTypeRegistry messageTypeRegistry;

    public MessageDeliveryModeStrategy(ConnectionConfiguration connectionConfiguration, IMessageTypeRegistry messageTypeRegistry)
    {
        this.connectionConfiguration = connectionConfiguration;
        this.messageTypeRegistry = messageTypeRegistry;
    }

    /// <inheritdoc />
    public byte GetDeliveryMode(Type messageType)
    {
        var isPersistent = messageTypeRegistry.GetOrAdd(messageType).IsPersistent ?? connectionConfiguration.PersistentMessages;
        return isPersistent ? MessageDeliveryMode.Persistent : MessageDeliveryMode.NonPersistent;
    }
}
