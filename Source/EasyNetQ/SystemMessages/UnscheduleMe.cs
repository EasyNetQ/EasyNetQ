using System;

namespace EasyNetQ.SystemMessages
{
#if !DOTNET5_4
    [Serializable]
#endif
    public class UnscheduleMe
    {
        public string CancellationKey { get; set; }
    }
}