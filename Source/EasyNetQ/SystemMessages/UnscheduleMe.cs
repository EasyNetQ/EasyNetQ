using System;

namespace EasyNetQ.SystemMessages
{
    [Serializable]
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}