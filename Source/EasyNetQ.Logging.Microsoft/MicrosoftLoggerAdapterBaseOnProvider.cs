using System;
using MicrosoftLogging = Microsoft.Extensions.Logging;
using MicrosoftLoggingExtensions = Microsoft.Extensions.Logging.LoggerExtensions;
using MicrosoftLoggingLoggerFactoryExtensions = Microsoft.Extensions.Logging.LoggerFactoryExtensions;

namespace EasyNetQ.Logging.Microsoft
{
    /// <summary>
    ///     Creates ILogger
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        ///     Creates ILogger based on category
        /// </summary>
        /// <typeparam name="TCategory"></typeparam>
        ILogger Create<TCategory>();

        /// <summary>
        ///     Creates ILogger based on category
        /// </summary>
        ILogger Create();
    }

    /// <inheritdoc />
    public class LoggerFactory : ILoggerFactory
    {
        private readonly MicrosoftLogging.ILoggerFactory loggerFactory;

        /// <summary>
        ///     Creates LoggerFactory on top of Microsoft.Extensions.Logger.ILoggerFactory
        /// </summary>
        /// <param name="loggerFactory"></param>
        public LoggerFactory(MicrosoftLogging.ILoggerFactory loggerFactory)
        {
            this.loggerFactory = loggerFactory;
        }


        /// <inheritdoc />
        public ILogger Create<TCategory>()
        {
            var microsoftLogger = MicrosoftLoggingLoggerFactoryExtensions.CreateLogger<TCategory>(loggerFactory);
            return new MicrosoftLoggerAdapter<TCategory>(microsoftLogger);
        }

        /// <inheritdoc />
        public ILogger Create()
        {
            var microsoftLogger = loggerFactory.CreateLogger("EasyNetQ");
            return new MicrosoftLoggerAdapter(microsoftLogger);
        }
    }

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
