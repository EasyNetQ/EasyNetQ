using System;

namespace EasyNetQ.SystemMessages
{
#if !NET_CORE
#endif
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}