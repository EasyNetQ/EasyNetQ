using System;

namespace EasyNetQ.AutoSubscribe
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SubscriptionConfigurationAttribute : Attribute
    {
        public bool AutoDelete { get;  set; }
        public int Priority { get;  set; }
        public ushort PrefetchCount { get;  set; }
        public int Expires { get; set; }
    }
}
