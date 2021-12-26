namespace EasyNetQ.Sprache;

internal delegate IResult<T> Parser<out T>(Input input);

internal static class ParserExtensions
{
    public static IResult<T> TryParse<T>(this Parser<T> parser, string input)
    {
        Preconditions.CheckNotNull(parser, nameof(parser));
        Preconditions.CheckNotNull(input, nameof(input));

        return parser(new Input(input));
    }

    public static T Parse<T>(this Parser<T> parser, string input)
    {
        Preconditions.CheckNotNull(parser, nameof(parser));
        Preconditions.CheckNotNull(input, nameof(input));

        var result = parser.TryParse(input);

        if (result is ISuccess<T> success)
        {
            if (!success.Remainder.AtEnd)
            {
                throw new ParseException(string.Format("Parsing failure: Couldn't parse the whole input; unparsable remainder is: \"{0}\".", success.Remainder.Source.Substring(success.Remainder.Position)));
            }

            return success.Result;
        }

        throw new ParseException(result.ToString());
    }
}
