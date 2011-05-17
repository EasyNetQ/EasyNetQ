using System.Collections.Generic;

namespace EasyNetQ.Monitor.Model
{
    public class MessageType
    {
        public virtual IList<Message> Messages { get; protected set; }
        public virtual string TypeName { get; protected set; }

        protected MessageType(){}

        public MessageType(string typeName)
        {
            Messages = new List<Message>();
            TypeName = typeName;
        }
    }
}