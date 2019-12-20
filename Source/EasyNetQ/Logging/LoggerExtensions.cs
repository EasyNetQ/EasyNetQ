using System;

namespace EasyNetQ.Logging
{
    static class LoggerExtensions
    {
        internal static readonly object[] EmptyParams = new object[0];

        public static bool IsDebugEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Debug, null, null, EmptyParams);
        }

        public static bool IsErrorEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Error, null, null, EmptyParams);
        }

        public static bool IsFatalEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Fatal, null, null, EmptyParams);
        }

        public static bool IsInfoEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Info, null, null, EmptyParams);
        }

        public static bool IsTraceEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Trace, null, null, EmptyParams);
        }

        public static bool IsWarnEnabled(this ILogger logger)
        {
            GuardAgainstNullLogger(logger);
            return logger.Log(LogLevel.Warn, null, null, EmptyParams);
        }

        public static void Debug(this ILogger logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Debug, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Debug(this ILogger logger, string message)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), null, EmptyParams);
        }

        public static void Debug(this ILogger logger, string message, params object[] args)
        {
            logger.DebugFormat(message, args);
        }

        public static void Debug(this ILogger logger, string message, object arg0)
        {
            if (logger.IsDebugEnabled()) logger.DebugFormat(message, arg0);
        }

        public static void Debug(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsDebugEnabled()) logger.DebugFormat(message, arg0, arg1);
        }

        public static void Debug(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsDebugEnabled()) logger.DebugFormat(message, arg0, arg1, arg2);
        }

        public static void Debug(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.DebugException(message, exception, args);
        }

        public static void Debug(this ILogger logger, Exception exception, string message, object arg0)
        {
            if (logger.IsDebugEnabled()) logger.DebugException(message, exception, arg0);
        }

        public static void DebugFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsDebugEnabled()) logger.LogFormat(LogLevel.Debug, message, args);
        }

        public static void DebugFormat(this ILogger logger, string message, object arg0)
        {
            if (logger.IsDebugEnabled()) logger.LogFormat(LogLevel.Debug, message, arg0);
        }

        public static void DebugFormat(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsDebugEnabled()) logger.LogFormat(LogLevel.Debug, message, arg0, arg1);
        }

        public static void DebugFormat(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsDebugEnabled()) logger.LogFormat(LogLevel.Debug, message, arg0, arg1, arg2);
        }

        public static void DebugException(this ILogger logger, string message, Exception exception)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), exception, EmptyParams);
        }

        public static void DebugException(this ILogger logger, string message, Exception exception, object formatParam0)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), exception, formatParam0);
        }

        public static void DebugException(this ILogger logger, string message, Exception exception, object formatParam0,
            object formatParam1)
        {
            if (logger.IsDebugEnabled())
                logger.Log(LogLevel.Debug, message.AsFunc(), exception, formatParam0, formatParam1);
        }

        public static void DebugException(this ILogger logger, string message, Exception exception, object formatParam0,
            object formatParam1, object formatParam2)
        {
            if (logger.IsDebugEnabled())
                logger.Log(LogLevel.Debug, message.AsFunc(), exception, formatParam0, formatParam1, formatParam2);
        }

        public static void DebugException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsDebugEnabled()) logger.Log(LogLevel.Debug, message.AsFunc(), exception, formatParams);
        }

        public static void Error(this ILogger logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Error, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Error(this ILogger logger, string message)
        {
            if (logger.IsErrorEnabled()) logger.Log(LogLevel.Error, message.AsFunc(), null, EmptyParams);
        }

        public static void Error(this ILogger logger, string message, params object[] args)
        {
            logger.ErrorFormat(message, args);
        }

        public static void Error(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.ErrorException(message, exception, args);
        }

        public static void ErrorFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsErrorEnabled()) logger.LogFormat(LogLevel.Error, message, args);
        }

        public static void ErrorException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsErrorEnabled()) logger.Log(LogLevel.Error, message.AsFunc(), exception, formatParams);
        }

        public static void Fatal(this ILogger logger, Func<string> messageFunc)
        {
            logger.Log(LogLevel.Fatal, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Fatal(this ILogger logger, string message)
        {
            if (logger.IsFatalEnabled()) logger.Log(LogLevel.Fatal, message.AsFunc(), null, EmptyParams);
        }

        public static void Fatal(this ILogger logger, string message, params object[] args)
        {
            logger.FatalFormat(message, args);
        }

        public static void Fatal(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.FatalException(message, exception, args);
        }

        public static void FatalFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsFatalEnabled()) logger.LogFormat(LogLevel.Fatal, message, args);
        }

        public static void FatalException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsFatalEnabled()) logger.Log(LogLevel.Fatal, message.AsFunc(), exception, formatParams);
        }

        public static void Info(this ILogger logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Info, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Info(this ILogger logger, string message)
        {
            if (logger.IsInfoEnabled()) logger.Log(LogLevel.Info, message.AsFunc(), null, EmptyParams);
        }

        public static void Info(this ILogger logger, string message, params object[] args)
        {
            logger.InfoFormat(message, args);
        }

        public static void Info(this ILogger logger, string message, object arg0)
        {
            if (logger.IsInfoEnabled()) logger.InfoFormat(message, arg0);
        }

        public static void Info(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsInfoEnabled()) logger.InfoFormat(message, arg0, arg1);
        }

        public static void Info(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsInfoEnabled()) logger.InfoFormat(message, arg0, arg1, arg2);
        }

        public static void Info(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.InfoException(message, exception, args);
        }

        public static void Info(this ILogger logger, Exception exception, string message, object arg0)
        {
            if (logger.IsInfoEnabled()) logger.InfoException(message, exception, arg0);
        }

        public static void Info(this ILogger logger, Exception exception, string message, object arg0, object arg1)
        {
            if (logger.IsInfoEnabled()) logger.InfoException(message, exception, arg0, arg1);
        }

        public static void Info(this ILogger logger, Exception exception, string message, object arg0, object arg1,
            object arg2)
        {
            if (logger.IsInfoEnabled()) logger.InfoException(message, exception, arg0, arg1, arg2);
        }

        public static void InfoFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsInfoEnabled()) logger.LogFormat(LogLevel.Info, message, args);
        }

        public static void InfoFormat(this ILogger logger, string message, object arg0)
        {
            if (logger.IsInfoEnabled()) logger.LogFormat(LogLevel.Info, message, arg0);
        }

        public static void InfoFormat(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsInfoEnabled()) logger.LogFormat(LogLevel.Info, message, arg0, arg1);
        }

        public static void InfoFormat(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsInfoEnabled()) logger.LogFormat(LogLevel.Info, message, arg0, arg1, arg2);
        }

        public static void InfoException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsInfoEnabled()) logger.Log(LogLevel.Info, message.AsFunc(), exception, formatParams);
        }

        public static void Trace(this ILogger logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Trace, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Trace(this ILogger logger, string message)
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, message.AsFunc(), null, EmptyParams);
        }

        public static void Trace(this ILogger logger, string message, params object[] args)
        {
            logger.TraceFormat(message, args);
        }

        public static void Trace(this ILogger logger, string message, object arg0)
        {
            if (logger.IsTraceEnabled()) logger.TraceFormat(message, arg0);
        }

        public static void Trace(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsTraceEnabled()) logger.TraceFormat(message, arg0, arg1);
        }

        public static void Trace(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsTraceEnabled()) logger.TraceFormat(message, arg0, arg1, arg2);
        }

        public static void Trace(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.TraceException(message, exception, args);
        }

        public static void Trace(this ILogger logger, Exception exception, string message, object arg0)
        {
            if (logger.IsTraceEnabled()) logger.TraceException(message, exception, arg0);
        }

        public static void Trace(this ILogger logger, Exception exception, string message, object arg0, object arg1)
        {
            if (logger.IsTraceEnabled()) logger.TraceException(message, exception, arg0, arg1);
        }

        public static void Trace(this ILogger logger, Exception exception, string message, object arg0, object arg1,
            object arg2)
        {
            if (logger.IsTraceEnabled()) logger.TraceException(message, exception, arg0, arg1, arg2);
        }

        public static void TraceFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsTraceEnabled()) logger.LogFormat(LogLevel.Trace, message, args);
        }

        public static void TraceFormat(this ILogger logger, string message, object arg0)
        {
            if (logger.IsTraceEnabled()) logger.LogFormat(LogLevel.Trace, message, arg0);
        }

        public static void TraceFormat(this ILogger logger, string message, object arg0, object arg1)
        {
            if (logger.IsTraceEnabled()) logger.LogFormat(LogLevel.Trace, message, arg0, arg1);
        }

        public static void TraceFormat(this ILogger logger, string message, object arg0, object arg1, object arg2)
        {
            if (logger.IsTraceEnabled()) logger.LogFormat(LogLevel.Trace, message, arg0, arg1, arg2);
        }

        public static void TraceException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, message.AsFunc(), exception, formatParams);
        }

        public static void TraceException(this ILogger logger, string message, Exception exception, object formatParam0)
        {
            if (logger.IsTraceEnabled()) logger.Log(LogLevel.Trace, message.AsFunc(), exception, formatParam0);
        }

        public static void TraceException(this ILogger logger, string message, Exception exception, object formatParam0,
            object formatParam1)
        {
            if (logger.IsTraceEnabled())
                logger.Log(LogLevel.Trace, message.AsFunc(), exception, formatParam0, formatParam1);
        }

        public static void TraceException(this ILogger logger, string message, Exception exception, object formatParam0,
            object formatParam1, object formatParam2)
        {
            if (logger.IsTraceEnabled())
                logger.Log(LogLevel.Trace, message.AsFunc(), exception, formatParam0, formatParam1, formatParam2);
        }

        public static void Warn(this ILogger logger, Func<string> messageFunc)
        {
            GuardAgainstNullLogger(logger);
            logger.Log(LogLevel.Warn, WrapLogInternal(messageFunc), null, EmptyParams);
        }

        public static void Warn(this ILogger logger, string message)
        {
            if (logger.IsWarnEnabled()) logger.Log(LogLevel.Warn, message.AsFunc(), null, EmptyParams);
        }

        public static void Warn(this ILogger logger, string message, params object[] args)
        {
            logger.WarnFormat(message, args);
        }

        public static void Warn(this ILogger logger, Exception exception, string message, params object[] args)
        {
            logger.WarnException(message, exception, args);
        }

        public static void WarnFormat(this ILogger logger, string message, params object[] args)
        {
            if (logger.IsWarnEnabled()) logger.LogFormat(LogLevel.Warn, message, args);
        }

        public static void WarnException(this ILogger logger, string message, Exception exception,
            params object[] formatParams)
        {
            if (logger.IsWarnEnabled()) logger.Log(LogLevel.Warn, message.AsFunc(), exception, formatParams);
        }

        // ReSharper disable once UnusedParameter.Local
        private static void GuardAgainstNullLogger(ILogger logger)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
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

        // Allow passing callsite-logger-type to LogProviderBase using messageFunc
        internal static Func<string> WrapLogSafeInternal(LoggerExecutionWrapper logger, Func<string> messageFunc)
        {
            string WrappedMessageFunc()
            {
                try
                {
                    return messageFunc();
                }
                catch (Exception ex)
                {
                    logger.WrappedLogger(LogLevel.Error, () => LoggerExecutionWrapper.FailedToGenerateLogMessage, ex,
                        EmptyParams);
                }

                return null;
            }

            return WrappedMessageFunc;
        }

        // Allow passing callsite-logger-type to LogProviderBase using messageFunc
        private static Func<string> WrapLogInternal(Func<string> messageFunc)
        {
            string WrappedMessageFunc()
            {
                return messageFunc();
            }

            return WrappedMessageFunc;
        }
    }
}
