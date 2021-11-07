using System;

namespace EasyNetQ.Logging
{
    /// <inheritdoc />
    public class NoopLogger : ILogger
    {
        /// <inheritdoc />
        public bool Log(
            LogLevel logLevel,
            Func<string> messageFunc,
            Exception exception = null,
            params object[] formatParameters
        )
        {
            return false;
        }
    }

    /// <inheritdoc />
    public class NoopLogger<TCategoryName> : ILogger<TCategoryName>
    {
        /// <inheritdoc />
        public bool Log(
            LogLevel logLevel,
            Func<string> messageFunc,
            Exception exception = null,
            params object[] formatParameters
        )
        {
            return false;
        }
    }
}
