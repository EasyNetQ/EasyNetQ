using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyNetQ
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false)]
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
