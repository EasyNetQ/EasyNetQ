using System;
using System.Linq;
using System.Reflection;

namespace EasyNetQ.AutoSubscribe;

public class AutoSubscriberConsumerInfo
{
    public Type ConcreteType { get; }
    public Type MessageType { get; }
    public MethodInfo ConsumeMethod { get; }

    public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
    {
        Preconditions.CheckNotNull(concreteType, nameof(concreteType));
        Preconditions.CheckNotNull(interfaceType, nameof(interfaceType));
        Preconditions.CheckNotNull(messageType, nameof(messageType));

        ConcreteType = concreteType;
        MessageType = messageType;
        ConsumeMethod = ConcreteType.GetInterfaceMap(interfaceType).TargetMethods.Single();
    }
}
