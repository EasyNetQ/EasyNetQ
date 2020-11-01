using EasyNetQ.Logging.LogProviders;
using System;
using System.Collections.Generic;

namespace EasyNetQ.Logging
{
    public class ConsoleLogProvider : ILogProvider
    {
        private static readonly object SyncRoot = new object();

        private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new Dictionary<LogLevel, ConsoleColor>
            {
                { LogLevel.Fatal, ConsoleColor.Red },
                { LogLevel.Error, ConsoleColor.Yellow },
                { LogLevel.Warn, ConsoleColor.Magenta },
                { LogLevel.Info, ConsoleColor.White },
                { LogLevel.Debug, ConsoleColor.Gray },
                { LogLevel.Trace, ConsoleColor.DarkGray },
            };

        public static ConsoleLogProvider Instance = new ConsoleLogProvider();

        private ConsoleLogProvider()
        {
        }

        /// <inheritdoc />
        public Logger GetLogger(string name)
        {
            return (logLevel, messageFunc, exception, formatParameters) =>
            {
                if (messageFunc == null)
                {
                    return true;
                }

                lock (SyncRoot)
                {
                    var consoleColor = Colors[logLevel];
                    var originalForeground = Console.ForegroundColor;
                    try
                    {
                        Console.ForegroundColor = consoleColor;
                        WriteMessage(logLevel, name, messageFunc, formatParameters, exception);
                    }
                    finally
                    {
                        Console.ForegroundColor = originalForeground;
                    }
                }

                return true;
            };
        }

        private static void WriteMessage(
            LogLevel logLevel,
            string name,
            Func<string> messageFunc,
            object[] formatParameters,
            Exception exception)
        {
            var formattedMessage = LogMessageFormatter.FormatStructuredMessage(messageFunc(), formatParameters, out _);

            if (exception != null)
            {
                formattedMessage = formattedMessage + " -> " + exception;
            }

            Console.WriteLine("[{0:HH:mm:ss} {1}] {2} {3}", DateTime.UtcNow, logLevel, name, formattedMessage);
        }

        /// <inheritdoc />
        public IDisposable OpenNestedContext(string message)
        {
            return NullDisposable.Instance;
        }

        /// <inheritdoc />
        public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
        {
            return NullDisposable.Instance;
        }

        private class NullDisposable : IDisposable
        {
            internal static readonly IDisposable Instance = new NullDisposable();

            public void Dispose()
            { }
        }
    }
}
