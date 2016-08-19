using System;

namespace EasyNetQ
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
    public class TimeoutSecondsAttribute : Attribute
    {
        public ushort Timeout { get; private set; }

        public TimeoutSecondsAttribute(ushort timeout)
        {
            Timeout = timeout;
        }
    }
}