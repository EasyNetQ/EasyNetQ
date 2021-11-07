using System;
using MicrosoftLogging = Microsoft.Extensions.Logging;
using MicrosoftLoggingExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace EasyNetQ.Logging.Microsoft
{
    /// <inheritdoc />
    public class MicrosoftLoggerAdapter : ILogger
    {
        private readonly MicrosoftLogging.ILogger logger;

        /// <summary>
        ///     Creates an adapter on top of Microsoft.Extensions.Logging.ILogger
        /// </summary>
        /// <param name="logger"></param>
        public MicrosoftLoggerAdapter(MicrosoftLogging.ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc />
        public bool Log(
            LogLevel logLevel,
            Func<string> messageFunc,
            Exception exception = null,
            params object[] formatParameters
        )
        {
            var microsoftLogLevel = logLevel switch
            {
                LogLevel.Debug => MicrosoftLogging.LogLevel.Debug,
                LogLevel.Error => MicrosoftLogging.LogLevel.Error,
                LogLevel.Fatal => MicrosoftLogging.LogLevel.Critical,
                LogLevel.Info => MicrosoftLogging.LogLevel.Information,
                LogLevel.Trace => MicrosoftLogging.LogLevel.Trace,
                LogLevel.Warn => MicrosoftLogging.LogLevel.Warning,
                _ => MicrosoftLogging.LogLevel.None
            };

            if (messageFunc == null)
            {
                return logger.IsEnabled(microsoftLogLevel);
            }

            var message = messageFunc();

            if (exception != null)
            {
                MicrosoftLoggingExtensions.Log(logger, microsoftLogLevel, exception, message, formatParameters);
            }
            else
            {
                MicrosoftLoggingExtensions.Log(logger, microsoftLogLevel, message, formatParameters);
            }

            return true;
        }
    }

    /// <inheritdoc cref="EasyNetQ.Logging.Microsoft.MicrosoftLoggerAdapter" />
    public class MicrosoftLoggerAdapter<TCategory> : MicrosoftLoggerAdapter, ILogger<TCategory>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public MicrosoftLoggerAdapter(MicrosoftLogging.ILogger<TCategory> logger) : base(logger)
        {
        }
    }
}
