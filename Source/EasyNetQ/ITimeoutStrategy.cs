using System;
using System.Linq;

namespace EasyNetQ
{
    public interface ITimeoutStrategy
    {
        ulong GetTimeoutSeconds(Type messageType);
    }

    public class TimeoutStrategy : ITimeoutStrategy
    {
        private readonly ConnectionConfiguration connectionConfiguration;

        public TimeoutStrategy(ConnectionConfiguration connectionConfiguration)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            this.connectionConfiguration = connectionConfiguration;
        }

        public ulong GetTimeoutSeconds(Type messageType)
        {
            Preconditions.CheckNotNull(messageType, "messageType");
            var timeoutAttribute = messageType.GetAttributes<TimeoutSecondsAttribute>().FirstOrDefault();
            if (timeoutAttribute != null)
                return timeoutAttribute.Timeout;
            return connectionConfiguration.Timeout;
        }
    }
}