using System;

namespace EasyNetQ
{
#if !NET_CORE
#endif
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
