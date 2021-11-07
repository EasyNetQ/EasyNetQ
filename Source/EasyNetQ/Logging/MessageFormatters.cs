using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyNetQ.Logging
{
    internal static class MessageFormatter
    {
        private static readonly Regex Pattern = new(@"(?<!{){@?(?<arg>[^ :{}]+)(?<format>:[^}]+)?}", RegexOptions.Compiled);

        /// <summary>
        ///     Some logging frameworks support structured logging, such as serilog. This will allow you to add names to structured
        ///     data in a format string:
        ///     For example: Log("Log message to {user}", user). This only works with serilog, but as the user of LibLog, you don't
        ///     know if serilog is actually
        ///     used. So, this class simulates that. it will replace any text in {curly braces} with an index number.
        ///     "Log {message} to {user}" would turn into => "Log {0} to {1}". Then the format parameters are handled using regular
        ///     .net string.Format.
        /// </summary>
        /// <param name="messageBuilder">The message builder.</param>
        /// <param name="formatParameters">The format parameters.</param>
        /// <returns></returns>
        public static Func<string> SimulateStructuredLogging(Func<string> messageBuilder, object[] formatParameters)
        {
            if (formatParameters == null || formatParameters.Length == 0) return messageBuilder;

            return () =>
            {
                var targetMessage = messageBuilder();
                IEnumerable<string> _;
                return FormatStructuredMessage(targetMessage, formatParameters, out _);
            };
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search, StringComparison.Ordinal);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static string FormatStructuredMessage(
            string targetMessage, object[] formatParameters, out IEnumerable<string> patternMatches
            )
        {
            if (formatParameters == null || formatParameters.Length == 0)
            {
                patternMatches = Enumerable.Empty<string>();
                return targetMessage;
            }

            List<string> processedArguments = null;

            foreach (Match match in Pattern.Matches(targetMessage))
            {
                var arg = match.Groups["arg"].Value;

                if (!int.TryParse(arg, out _))
                {
                    processedArguments ??= new List<string>(formatParameters.Length);
                    var argumentIndex = processedArguments.IndexOf(arg);
                    if (argumentIndex == -1)
                    {
                        argumentIndex = processedArguments.Count;
                        processedArguments.Add(arg);
                    }

                    targetMessage = ReplaceFirst(
                        targetMessage,
                        match.Value,
                        string.Concat("{", argumentIndex.ToString(), match.Groups["format"].Value, "}")
                    );
                }
            }

            patternMatches = processedArguments ?? Enumerable.Empty<string>();

            try
            {
                return string.Format(CultureInfo.InvariantCulture, targetMessage, formatParameters);
            }
            catch (FormatException exception)
            {
                throw new FormatException(
                    "The input string '" + targetMessage + "' could not be formatted using string.Format", exception
                );
            }
        }
    }
}
