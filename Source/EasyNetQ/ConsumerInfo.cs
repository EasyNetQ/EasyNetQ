using System;

namespace EasyNetQ
{
    [Serializable]
    public class ConsumerInfo
    {
        public readonly Type ConcreteType;
        public readonly Type InterfaceType;
        public readonly Type MessageType;

        public ConsumerInfo(Type concreteType, Type interfaceType, Type messageType)
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