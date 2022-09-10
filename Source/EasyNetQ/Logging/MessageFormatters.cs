using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyNetQ.Logging;

internal static class MessageFormatter
{
    private static readonly Regex Pattern = new(@"(?<!{){@?(?<arg>[^ :{}]+)(?<format>:[^}]+)?}", RegexOptions.Compiled);

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
        string targetMessage, object?[]? formatParameters, out IEnumerable<string> patternMatches
    )
    {
        if (formatParameters == null || formatParameters.Length == 0)
        {
            patternMatches = Enumerable.Empty<string>();
            return targetMessage;
        }

        List<string>? processedArguments = null;

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
