using System;
using System.Collections.Generic;

namespace EasyNetQ.Monitor.Model
{
    [Serializable]
    public class VHost
    {
        public virtual IList<MessageType> MessageTypes { get; protected set; }
        public virtual string Name { get; protected set; }

        protected VHost(){}

        public VHost(string name)
        {
            MessageTypes = new List<MessageType>();
            Name = name;
        }

        public virtual MessageType CreateMessageType(string typeName)
        {
            var messageType = new MessageType(typeName);
            MessageTypes.Add(messageType);
            return messageType;
        }
    }
}