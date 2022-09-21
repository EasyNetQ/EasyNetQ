namespace EasyNetQ.Sprache;

internal delegate IResult<T> Parser<out T>(Input input);

internal static class ParserExtensions
{
    public static IResult<T> TryParse<T>(this Parser<T> parser, string input) => parser(new Input(input));

    public static T Parse<T>(this Parser<T> parser, string input)
    {
        var result = parser.TryParse(input);

        if (result is ISuccess<T> success)
        {
            if (success.Remainder.AtEnd)
                return success.Result;

            var unparsableReminder = success.Remainder.Source.Substring(success.Remainder.Position);
            throw new ParseException($"Parsing failure: Couldn't parse the whole input; unparsable remainder is: \"{unparsableReminder}\".");
        }

        throw new ParseException(result.ToString() ?? "Unknown failure");
    }
}
