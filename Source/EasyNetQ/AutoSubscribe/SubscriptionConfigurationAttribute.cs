using System;

namespace EasyNetQ.AutoSubscribe
{
#if !NET_CORE
#endif
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class SubscriptionConfigurationAttribute : Attribute
    {
        public bool AutoDelete { get;  set; }
        public int Priority { get;  set; }
        public bool CancelOnHaFailover { get;  set; }
        public ushort PrefetchCount { get;  set; }
        public int Expires { get; set; }
    }
}
