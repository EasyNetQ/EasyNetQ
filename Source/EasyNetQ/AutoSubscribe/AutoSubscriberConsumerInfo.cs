using System.Reflection;

namespace EasyNetQ.AutoSubscribe;

public class AutoSubscriberConsumerInfo
{
    public Type ConcreteType { get; }
    public Type MessageType { get; }
    public MethodInfo ConsumeMethod { get; }

    public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
    {
        ConcreteType = concreteType;
        MessageType = messageType;
        ConsumeMethod = ConcreteType.GetInterfaceMap(interfaceType).TargetMethods.Single();
    }
}
