using System;

namespace EasyNetQ.AutoSubscribe
{
#if !DOTNET5_4
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
