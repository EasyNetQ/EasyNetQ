using System;

namespace EasyNetQ.AutoSubscribe
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoSubscriberConsumerAttribute : Attribute
    {
        public string SubscriptionId { get; set; }
    }
}