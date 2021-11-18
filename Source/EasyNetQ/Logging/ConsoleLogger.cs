using System;
using System.Collections.Generic;

namespace EasyNetQ.Logging
{
    /// <inheritdoc />
    public class ConsoleLogger : ILogger
    {
        private static readonly object SyncRoot = new();

        private static readonly Dictionary<LogLevel, ConsoleColor> Colors = new()
        {
            { LogLevel.Fatal, ConsoleColor.Red },
            { LogLevel.Error, ConsoleColor.Yellow },
            { LogLevel.Warn, ConsoleColor.Magenta },
            { LogLevel.Info, ConsoleColor.White },
            { LogLevel.Debug, ConsoleColor.Gray },
            { LogLevel.Trace, ConsoleColor.DarkGray }
        };

        /// <inheritdoc />
        public bool Log(
            LogLevel logLevel,
            Func<string> messageFunc,
            Exception exception = null,
            params object[] formatParameters
        )
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

                    var formattedMessage = MessageFormatter.FormatStructuredMessage(
                        messageFunc(), formatParameters, out _
                    );

                    if (exception != null)
                    {
                        formattedMessage = formattedMessage + " -> " + exception;
                    }

                    Console.WriteLine("[{0:HH:mm:ss} {1}] {2}", DateTime.UtcNow, logLevel, formattedMessage);
                }
                finally
                {
                    Console.ForegroundColor = originalForeground;
                }
            }

            return true;
        }
    }

    /// <inheritdoc cref="EasyNetQ.Logging.ConsoleLogger" />
    public class ConsoleLogger<TCategoryName> : ConsoleLogger, ILogger<TCategoryName>
    {
    }
}
