using System;

namespace EasyNetQ.SystemMessages
{
#if !NET_CORE
    [Serializable]
#endif
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}