namespace Microsoft.Extensions.Logging;

/// <summary>
///     Extension methods for the <see cref="ILogger"/> interface.
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    ///     Check if the <see cref="LogLevel.Debug"/> log level is enabled.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
    /// <returns>True if the log level is enabled; false otherwise.</returns>
    public static bool IsDebugEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Debug);

    /// <summary>
    ///     Check if the <see cref="LogLevel.Info"/> log level is enabled.
    /// </summary>
    /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
    /// <returns>True if the log level is enabled; false otherwise.</returns>
    public static bool IsInfoEnabled(this ILogger logger) => logger.IsEnabled(LogLevel.Information);
}
