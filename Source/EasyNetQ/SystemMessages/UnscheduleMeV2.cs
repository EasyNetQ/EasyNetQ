using System;

namespace EasyNetQ.SystemMessages
{
    [Serializable]
    public class UnscheduleMeV2
    {
        public string CancellationKey { get; set; }
    }
}