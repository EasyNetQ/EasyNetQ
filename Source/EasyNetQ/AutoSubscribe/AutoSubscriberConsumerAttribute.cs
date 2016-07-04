using System;

namespace EasyNetQ.AutoSubscribe
{
#if !NET_CORE
#endif
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoSubscriberConsumerAttribute : Attribute
    {
        public string SubscriptionId { get; set; }
    }
}