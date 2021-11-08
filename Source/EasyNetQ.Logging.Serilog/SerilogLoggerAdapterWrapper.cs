using System;
using S = Serilog;

namespace EasyNetQ.Logging.Serilog
{
    /// <inheritdoc />
    public class SerilogLoggerAdapterWrapper : ILogger
    {
        private readonly ILogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggerFactory"></param>
        public SerilogLoggerAdapterWrapper(ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.Create();
        }

        /// <inheritdoc />
        public bool Log(LogLevel logLevel, Func<string> messageFunc, Exception exception = null, params object[] formatParameters)
        {
            return logger.Log(logLevel, messageFunc, exception, formatParameters);
        }
    }

    /// <inheritdoc cref="EasyNetQ.Logging.Serilog.SerilogLoggerAdapterWrapper" />
    public class SerilogLoggerAdapterWrapper<TCategory> : ILogger<TCategory>
    {
        private readonly ILogger logger;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggerFactory"></param>
        public SerilogLoggerAdapterWrapper(ILoggerFactory loggerFactory)
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
