namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class DeliveryModeAttribute : Attribute
{
    public DeliveryModeAttribute(bool isPersistent) => IsPersistent = isPersistent;

    public bool IsPersistent { get; }
}
