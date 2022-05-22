using System;

namespace EasyNetQ
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class QueueAttribute : Attribute
    {
        internal static readonly QueueAttribute Default = new(null);

        public QueueAttribute(string queueName)
        {
            QueueName = queueName ?? string.Empty;
        }

        public string QueueName { get; }

        public string ExchangeName { get; set; }

        public string QueueType { get; set; }
    }
}
