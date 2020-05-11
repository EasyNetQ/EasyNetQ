using System;

namespace EasyNetQ
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple=false)]
    public class QueueAttribute : Attribute
    {
        internal static readonly QueueAttribute Default = new QueueAttribute(null);

        public QueueAttribute(string queueName)
        {
            QueueName = queueName ?? string.Empty;
        }

        public string QueueName { get; }

        public string ExchangeName { get; set; }
    }
}
