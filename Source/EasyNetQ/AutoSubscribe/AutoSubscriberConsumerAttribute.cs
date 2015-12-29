using System;

namespace EasyNetQ.AutoSubscribe
{
#if !DOTNET5_4
    [Serializable]
#endif
    [AttributeUsage(AttributeTargets.Method)]
    public class AutoSubscriberConsumerAttribute : Attribute
    {
        public string SubscriptionId { get; set; }
    }
}