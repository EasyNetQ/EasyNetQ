using System;

namespace EasyNetQ.MessageVersioning
{
    public class MessageType
    {
        public string TypeString { get; set; }
        public Type Type { get; set; }
    }
}