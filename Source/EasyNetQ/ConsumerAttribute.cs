using System;

namespace EasyNetQ
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsumerAttribute : Attribute
    {
        public string SubscriptionId { get; set; }
    }
}