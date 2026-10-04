namespace EasyNetQ;

/// <summary>
///     Populates a freshly created <see cref="MessageTypeDescriptor" /> from the type's [Queue]/[Exchange]/
///     [DeliveryMode] attributes. This runs once per type when the registry creates the descriptor and is - next to
///     <see cref="RuntimeDescriptorFactory" /> - the runtime-reflection fallback that the source generator makes
///     unreachable by emitting the same values as generated registrations. GetCustomAttributes(inherit: true)
///     matches the 8.x ReflectionHelpers semantics (attributes inherited from base classes).
/// </summary>
internal static class AttributeMetadataReader
{
    public static void Populate(MessageTypeDescriptor descriptor)
    {
        foreach (var attribute in descriptor.Type.GetCustomAttributes(true))
        {
            switch (attribute)
            {
                case QueueAttribute queue:
                    descriptor.QueueName ??= queue.Name;
                    descriptor.QueueType ??= queue.Type;
                    break;
                case ExchangeAttribute exchange:
                    descriptor.ExchangeName ??= exchange.Name;
                    descriptor.ExchangeType ??= exchange.ExchangeType;
                    break;
                case DeliveryModeAttribute deliveryMode:
                    descriptor.IsPersistent ??= deliveryMode.IsPersistent;
                    break;
            }
        }
    }
}
