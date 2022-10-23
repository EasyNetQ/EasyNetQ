using System;
using S = Serilog;

namespace EasyNetQ.Logging.Serilog;

/// <inheritdoc />
public class SerilogLoggerAdapter<TCategory> : ILogger<TCategory>
{
    private readonly S.ILogger logger;

    /// <summary>
    ///     Creates an adapter on top of Serilog.ILogger
    /// </summary>
    /// <param name="logger"></param>
    // ReSharper disable once ContextualLoggerProblem
    public SerilogLoggerAdapter(S.ILogger logger) => this.logger = logger.ForContext<TCategory>();

    /// <inheritdoc />
    public virtual bool Log(
        LogLevel logLevel,
        Func<string>? messageFunc,
        Exception? exception = null,
        params object?[] formatParameters
    )
    {
        var serilogLogLevel = logLevel switch
        {
            LogLevel.Debug => S.Events.LogEventLevel.Debug,
            LogLevel.Error => S.Events.LogEventLevel.Error,
            LogLevel.Fatal => S.Events.LogEventLevel.Fatal,
            LogLevel.Info => S.Events.LogEventLevel.Information,
            LogLevel.Trace => S.Events.LogEventLevel.Verbose,
            LogLevel.Warn => S.Events.LogEventLevel.Warning,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (messageFunc == null) return logger.IsEnabled(serilogLogLevel);

        logger.Write(serilogLogLevel, exception, messageFunc(), formatParameters);
        return true;
    }
}
