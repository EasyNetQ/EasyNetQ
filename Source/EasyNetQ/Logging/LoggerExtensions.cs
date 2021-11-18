using System;

namespace EasyNetQ.Logging
{
    /// <summary>
    ///     Extension methods for the <see cref="ILogger"/> interface.
    /// </summary>
    public static class LoggerExtensions
    {
        private static readonly object[] EmptyParams = Array.Empty<object>();

        /// <summary>
        ///     Check if the <see cref="LogLevel.Debug"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsDebugEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Debug, null, null, EmptyParams);
        }

        /// <summary>
        ///     Check if the <see cref="LogLevel.Error"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsErrorEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Error, null, null, EmptyParams);
        }

        /// <summary>
        ///     Check if the <see cref="LogLevel.Fatal"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsFatalEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Fatal, null, null, EmptyParams);
        }

        /// <summary>
        ///     Check if the <see cref="LogLevel.Info"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsInfoEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Info, null, null, EmptyParams);
        }

        /// <summary>
        ///     Check if the <see cref="LogLevel.Trace"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsTraceEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Trace, null, null, EmptyParams);
        }

        /// <summary>
        ///     Check if the <see cref="LogLevel.Warn"/> log level is enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to check with.</param>
        /// <returns>True if the log level is enabled; false otherwise.</returns>
        public static bool IsWarnEnabled(this ILogger logger)
        {
            return logger.Log(LogLevel.Warn, null, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Debug(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Debug(this ILogger logger, string message)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Debug(this ILogger logger, string message, params object[] args)
        {
            logger.DebugFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Debug(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.DebugException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void DebugFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsDebugEnabled()) logger.LogFormat(LogLevel.Debug, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        public static void DebugException(this ILogger logger, string message, Exception exception)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), exception, EmptyParams);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Debug"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void DebugException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Error(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsErrorEnabled()) logger.Log(LogLevel.Error, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Error(this ILogger logger, string message)
        {
            if (logger.IsErrorEnabled()) logger.Log(LogLevel.Error, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Error(this ILogger logger, string message, params object[] args)
        {
            logger.ErrorFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Error(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.ErrorException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void ErrorFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsErrorEnabled()) logger.LogFormat(LogLevel.Error, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Error"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void ErrorException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsErrorEnabled()) logger.Log(LogLevel.Error, message.AsFunc(), exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Fatal(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsFatalEnabled()) logger.Log(LogLevel.Fatal, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Fatal(this ILogger logger, string message)
        {
            if (logger.IsFatalEnabled()) logger.Log(LogLevel.Fatal, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Fatal(this ILogger logger, string message, params object[] args)
        {
            logger.FatalFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Fatal(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.FatalException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void FatalFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsFatalEnabled()) logger.LogFormat(LogLevel.Fatal, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Fatal"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void FatalException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsFatalEnabled()) logger.Log(LogLevel.Fatal, message.AsFunc(), exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Info(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsInfoEnabled()) logger.Log(LogLevel.Info, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Info(this ILogger logger, string message)
        {
            if (logger.IsInfoEnabled()) logger.Log(LogLevel.Info, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Info(this ILogger logger, string message, params object[] args)
        {
            logger.InfoFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Info(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.InfoException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void InfoFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsInfoEnabled()) logger.LogFormat(LogLevel.Info, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Info"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void InfoException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsInfoEnabled()) logger.Log(LogLevel.Info, message.AsFunc(), exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Trace(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Trace(this ILogger logger, string message)
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Trace(this ILogger logger, string message, params object[] args)
        {
            logger.TraceFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Trace(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.TraceException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void TraceFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsTraceEnabled()) logger.LogFormat(LogLevel.Trace, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Trace"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void TraceException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, message.AsFunc(), exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="messageFunc">The message function.</param>
        public static void Warn(this ILogger logger, Func<string> messageFunc)
        {
            if (logger.IsWarnEnabled()) logger.Log(LogLevel.Warn, messageFunc, null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        public static void Warn(this ILogger logger, string message)
        {
            if (logger.IsWarnEnabled()) logger.Log(LogLevel.Warn, message.AsFunc(), null, EmptyParams);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Warn(this ILogger logger, string message, params object[] args)
        {
            logger.WarnFormat(message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void Warn(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.WarnException(message, exception, args);
        }

        /// <summary>
        ///     Logs a message at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void WarnFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsWarnEnabled()) logger.LogFormat(LogLevel.Warn, message, args);
        }

        /// <summary>
        ///     Logs an exception at the <see cref="LogLevel.Warn"/> log level, if enabled.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger"/> to use.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <param name="args">Optional format parameters for the message.</param>
        public static void WarnException(
            this ILogger logger, string message, Exception exception, params object[] args
        )
        {
            if (logger.IsWarnEnabled()) logger.Log(LogLevel.Warn, message.AsFunc(), exception, args);
        }

        private static void LogFormat(this ILogger logger, LogLevel logLevel, string message, params object[] args)
        {
            logger.Log(logLevel, message.AsFunc(), null, args);
        }

        // Avoid the closure allocation, see https://gist.github.com/AArnott/d285feef75c18f6ecd2b
        private static Func<T> AsFunc<T>(this T value) where T : class
        {
            return value.Return;
        }

        private static T Return<T>(this T value)
        {
            return value;
        }
    }
}
