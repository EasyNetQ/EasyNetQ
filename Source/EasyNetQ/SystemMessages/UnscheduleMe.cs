using System;

namespace EasyNetQ.SystemMessages
{
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}