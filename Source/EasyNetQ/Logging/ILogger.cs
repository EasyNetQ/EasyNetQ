using System;

namespace EasyNetQ.Logging
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        ///     Log.
        /// </summary>
        /// <param name="logLevel">The log level</param>
        /// <param name="messageFunc">The message function</param>
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

    /// <inheritdoc />
    // ReSharper disable once UnusedTypeParameter
    public interface ILogger<out TCategoryName> : ILogger
    {
    }
}
