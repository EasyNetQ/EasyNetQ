using System;

namespace EasyNetQ.Logging
{
    public class DefaultLogger : ILogger
    {
        private readonly ILog logger = LogProvider.For<DefaultLogger>();

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            return logger.Log(logLevel, messageFunc, exception, formatParameters);
        }
    }

    public class DefaultLogger<TCategoryName> : ILogger<TCategoryName>
    {
        private readonly ILog logger = LogProvider.For<TCategoryName>();

        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            return logger.Log(logLevel, messageFunc, exception, formatParameters);
        }
    }
}
