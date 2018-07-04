using System;
using System.Linq;
using System.Reflection;

namespace EasyNetQ.AutoSubscribe
{
    public class AutoSubscriberConsumerInfo
    {
        public Type ConcreteType{ get; }
        public Type InterfaceType { get; }
        public Type MessageType { get; }
        public MethodInfo ConsumeMethod { get; }

        public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
        {
            Preconditions.CheckNotNull(concreteType, "concreteType");
            Preconditions.CheckNotNull(interfaceType, "interfaceType");
            Preconditions.CheckNotNull(messageType, "messageType");

            ConcreteType = concreteType;
            InterfaceType = interfaceType;
            MessageType = messageType;
            // get implementing method for interface implementation
            ConsumeMethod = ConcreteType.GetInterfaceMap(InterfaceType).TargetMethods.Single();
        }
    }
}