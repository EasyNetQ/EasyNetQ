using System;
using MS = Microsoft.Extensions.Logging;
using MSExtensions = Microsoft.Extensions.Logging.LoggerExtensions;
using MSFactoryExtensions = Microsoft.Extensions.Logging.LoggerFactoryExtensions;

namespace EasyNetQ.Logging.Microsoft
{
    /// <inheritdoc />
    public class MicrosoftLoggerAdapterWrapper : ILogger
    {
        private readonly ILogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggerFactory"></param>
        public MicrosoftLoggerAdapterWrapper(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.Create();
        }

        /// <inheritdoc />
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            return logger.Log(logLevel, messageFunc, exception, formatParameters);
        }
    }

    /// <inheritdoc cref="EasyNetQ.Logging.Microsoft.MicrosoftLoggerAdapter" />
    public class MicrosoftLoggerAdapterWrapper<TCategory> : ILogger<TCategory>
    {
        private readonly ILogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggerFactory"></param>
        public MicrosoftLoggerAdapterWrapper(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.Create<TCategory>();
        }

        /// <inheritdoc />
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            return logger.Log(logLevel, messageFunc, exception, formatParameters);
        }
    }
}
