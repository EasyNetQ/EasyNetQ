using System;

namespace EasyNetQ.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Logs the specified message with level and arguments.
        /// </summary>
        /// <param name="logLevel">The log level</param>
        /// <param name="messageFunc">The message function; null to just check if the specified log level is enabled.</param>
        /// <param name="exception">The exception</param>
        /// <param name="formatParameters">The format parameters</param>
        /// <returns>A boolean.</returns>
        bool Log(
            LogLevel logLevel,
            Func<string> messageFunc,
            Exception exception = null,
            params object[] formatParameters
        );
    }

    /// <summary>
    /// Typed logger that can be used with dependency injection frameworks.
    /// </summary>
    /// <typeparam name="TCategoryName">Typically sets the source name of the logs.</typeparam>
    // ReSharper disable once UnusedTypeParameter
    public interface ILogger<out TCategoryName> : ILogger
    {
    }
}
