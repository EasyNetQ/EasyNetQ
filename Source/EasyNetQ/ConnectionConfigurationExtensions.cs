using System;
using System.Threading;

namespace EasyNetQ
{
    // TODO Should migrate Timeout from ushort to TimeSpan
    internal static class ConnectionConfigurationExtensions
    {
        public static TimeSpan GetTimeout(this ConnectionConfiguration configuration)
        {
            return configuration.Timeout == 0 ? Timeout.InfiniteTimeSpan : TimeSpan.FromSeconds(configuration.Timeout);
        }
    }
}
