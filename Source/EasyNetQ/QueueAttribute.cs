using System;

namespace EasyNetQ
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=false)]
    public class QueueAttribute : Attribute
    {
        public QueueAttribute(string queueName)
        {
            QueueName = queueName ?? string.Empty;
        }

        public string QueueName { get; private set; }
        public string ExchangeName { get; set; }
    }
}
