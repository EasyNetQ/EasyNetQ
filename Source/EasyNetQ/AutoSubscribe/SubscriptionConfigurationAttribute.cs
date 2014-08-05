using System;

namespace EasyNetQ.AutoSubscribe
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Method)]
    public class SubscriptionConfigurationAttribute : Attribute
    {
        public SubscriptionConfigurationAttribute()
        {
            Expires = int.MaxValue;
        }

        public bool AutoDelete { get;  set; }
        public int Priority { get;  set; }
        public bool CancelOnHaFailover { get;  set; }
        public ushort PrefetchCount { get;  set; }
        public int Expires { get;  set; }
    }
}
