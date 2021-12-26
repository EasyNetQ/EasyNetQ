using System;
using MS = Microsoft.Extensions.Logging;
using MSExtensions = Microsoft.Extensions.Logging.LoggerExtensions;

namespace EasyNetQ.Logging.Microsoft;

/// <inheritdoc />
public class MicrosoftLoggerAdapter : ILogger
{
    private readonly MS.ILogger logger;

    /// <summary>
    ///     Creates an adapter on top of Microsoft.Extensions.Logging.ILogger
    /// </summary>
    /// <param name="logger"></param>
    public MicrosoftLoggerAdapter(MS.ILogger logger)
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
            LogLevel.Debug => MS.LogLevel.Debug,
            LogLevel.Error => MS.LogLevel.Error,
            LogLevel.Fatal => MS.LogLevel.Critical,
            LogLevel.Info => MS.LogLevel.Information,
            LogLevel.Trace => MS.LogLevel.Trace,
            LogLevel.Warn => MS.LogLevel.Warning,
            _ => MS.LogLevel.None
        };

        if (messageFunc == null)
        {
            return logger.IsEnabled(microsoftLogLevel);
        }

        var message = messageFunc();

        if (exception != null)
        {
            MSExtensions.Log(logger, microsoftLogLevel, exception, message, formatParameters);
        }
        else
        {
            MSExtensions.Log(logger, microsoftLogLevel, message, formatParameters);
        }

        return true;
    }
}

/// <inheritdoc cref="EasyNetQ.Logging.Microsoft.MicrosoftLoggerAdapter" />
public class MicrosoftLoggerAdapter<TCategory> : MicrosoftLoggerAdapter, ILogger<TCategory>
{
    /// <summary>
    ///     Creates an adapter on top of Microsoft.Extensions.Logging.ILogger
    /// </summary>
    /// <param name="logger"></param>
    public MicrosoftLoggerAdapter(MS.ILogger<TCategory> logger) : base(logger)
    {
    }
}
