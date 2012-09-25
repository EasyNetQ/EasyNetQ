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
            if (concreteType == null)
                throw new ArgumentNullException("concreteType");

            if (interfaceType == null)
                throw new ArgumentNullException("interfaceType");

            if (messageType == null)
                throw new ArgumentNullException("messageType");

            ConcreteType = concreteType;
            InterfaceType = interfaceType;
            MessageType = messageType;
        }
    }
}