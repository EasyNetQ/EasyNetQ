using System;

namespace EasyNetQ.AutoSubscribe
{
#if !DOTNET5_4
#endif
    public class AutoSubscriberConsumerInfo
    {
        public readonly Type ConcreteType;
        public readonly Type InterfaceType;
        public readonly Type MessageType;

        public AutoSubscriberConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
        {
            Preconditions.CheckNotNull(concreteType, "concreteType");
            Preconditions.CheckNotNull(interfaceType, "interfaceType");
            Preconditions.CheckNotNull(messageType, "messageType");

            ConcreteType = concreteType;
            InterfaceType = interfaceType;
            MessageType = messageType;
        }
    }
}