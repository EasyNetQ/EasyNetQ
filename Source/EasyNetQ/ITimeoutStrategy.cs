using System;

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
            var timeoutAttribute = messageType.GetAttribute<TimeoutSecondsAttribute>();
            return timeoutAttribute != null ? timeoutAttribute.Timeout : connectionConfiguration.Timeout;
        }
    }
}