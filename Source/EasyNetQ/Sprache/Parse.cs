namespace EasyNetQ.Sprache;

/// <summary>
/// Parsers and combinators.
/// </summary>
internal static class Parse
{
    public static readonly Parser<char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");
    public static readonly Parser<char> Numeric = Char(char.IsNumber, "numeric character");
    public static readonly Parser<string> NonNegativeNumber = Numeric.AtLeastOnce().Text();

    /// <summary>
    /// TryParse a single character matching 'predicate'
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public static Parser<char> Char(Predicate<char> predicate, string description)
    {
        return i =>
        {
            if (i.AtEnd)
                return new Failure<char>(i, () => "Unexpected end of input reached", () => new[] { description });

            if (predicate(i.Current))
                return new Success<char>(i.Current, i.Advance());

            return new Failure<char>(i, () => $"unexpected '{i.Current}'", () => new[] { description });

        };
    }

    /// <summary>
    /// Parse a single character except those matching <paramref name="predicate"/>.
    /// </summary>
    /// <param name="predicate">Characters not to match.</param>
    /// <param name="description">Description of characters that don't match.</param>
    /// <returns>A parser for characters except those matching <paramref name="predicate"/>.</returns>
    public static Parser<char> CharExcept(Predicate<char> predicate, string description) => Char(c => !predicate(c), "any character except " + description);

    /// <summary>
    /// Parse a single character c.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Parser<char> Char(char c) => Char(ch => c == ch, c.ToString());

    /// <summary>
    /// Parse a single character c.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Parser<char> CharCaseInsensitive(char c) => Char(ch => char.ToLower(c) == char.ToLower(ch), c.ToString());

    /// <summary>
    /// Parse a single character except c.
    /// </summary>
    /// <param name="c"></param>
    /// <returns></returns>
    public static Parser<char> CharExcept(char c) => CharExcept(ch => c == ch, c.ToString());

    /// <summary>
    /// Parse a string of characters.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Parser<IEnumerable<char>> String(string s)
    {
        return s
            .Select(Char)
            .Aggregate(Return(Enumerable.Empty<char>()), (a, p) => a.Concat(p.Once()))
            .Named(s);
    }

    /// <summary>
    /// Parse a string of characters.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    public static Parser<IEnumerable<char>> CaseInsensitiveString(string s)
    {
        return s
            .Select(CharCaseInsensitive)
            .Aggregate(Return(Enumerable.Empty<char>()), (a, p) => a.Concat(p.Once()))
            .Named(s);
    }

    /// <summary>
    /// Parse first, and if successful, then parse second.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static Parser<U> Then<T, U>(this Parser<T> first, Func<T, Parser<U>> second)
    {
        return i => first(i).IfSuccess(s => second(s.Result)(s.Remainder));
    }

    /// <summary>
    /// Parse a stream of elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <returns></returns>
    /// <remarks>Implemented imperatively to decrease stack usage.</remarks>
    public static Parser<IEnumerable<T>> Many<T>(this Parser<T> parser)
    {
        return i =>
        {
            var remainder = i;
            var result = new List<T>();
            var r = parser(i);
            while (r is ISuccess<T> success)
            {
                if (remainder == success.Remainder)
                    break;

                result.Add(success.Result);
                remainder = success.Remainder;
                r = parser(remainder);
            }

            return new Success<IEnumerable<T>>(result, remainder);
        };
    }


    /// <summary>
    /// TryParse a stream of elements with at least one item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static Parser<IEnumerable<T>> AtLeastOnce<T>(this Parser<T> parser)
    {
        return parser.Once().Then(t1 => parser.Many().Select(t1.Concat));
    }

    /// <summary>
    /// Take the result of parsing, and project it onto a different domain.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <param name="parser"></param>
    /// <param name="convert"></param>
    /// <returns></returns>
    public static Parser<U> Select<T, U>(this Parser<T> parser, Func<T, U> convert)
    {
        return parser.Then(t => Return(convert(t)));
    }

    /// <summary>
    /// Parse the token, embedded in any amount of whitespace characters.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static Parser<T> Token<T>(this Parser<T> parser)
    {
        return from leading in WhiteSpace.Many()
               from item in parser
               from trailing in WhiteSpace.Many()
               select item;
    }

    /// <summary>
    /// Convert a stream of characters to a string.
    /// </summary>
    /// <param name="characters"></param>
    /// <returns></returns>
    public static Parser<string> Text(this Parser<IEnumerable<char>> characters)
    {
        return characters.Select(chs => new string(chs.ToArray()));
    }

    /// <summary>
    /// Parse first, if it succeeds, return first, otherwise try second.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static Parser<T> Or<T>(this Parser<T> first, Parser<T> second)
    {
        return i =>
        {
            var fr = first(i);
            if (fr is IFailure<T> ff)
                return second(i).IfFailure(sf => new Failure<T>(ff.FailedInput, () => ff.Message, () => ff.Expectations.Union(sf.Expectations)));

            var fs = (ISuccess<T>)fr;
            return fs.Remainder == i ? second(i).IfFailure(_ => fs) : fs;
        };
    }

    /// <summary>
    /// Names part of the grammar for help with error messages.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Parser<T> Named<T>(this Parser<T> parser, string name)
    {
        return i => parser(i).IfFailure(f => f.FailedInput == i ? new Failure<T>(f.FailedInput, () => f.Message, () => new[] { name }) : f);
    }

    /// <summary>
    /// Parse a stream of elements containing only one item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="parser"></param>
    /// <returns></returns>
    public static Parser<IEnumerable<T>> Once<T>(this Parser<T> parser)
    {
        return parser.Select(r => (IEnumerable<T>)new[] { r });
    }

    /// <summary>
    /// Concatenate two streams of elements.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    public static Parser<IEnumerable<T>> Concat<T>(this Parser<IEnumerable<T>> first, Parser<IEnumerable<T>> second)
    {
        return first.Then(f => second.Select(f.Concat));
    }

    /// <summary>
    /// Succeed immediately and return value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static Parser<T> Return<T>(T value) => i => new Success<T>(value, i);

    /// <summary>
    /// Monadic combinator Then, adapted for Linq comprehension syntax.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    /// <typeparam name="V"></typeparam>
    /// <param name="parser"></param>
    /// <param name="selector"></param>
    /// <param name="projector"></param>
    /// <returns></returns>
    public static Parser<V> SelectMany<T, U, V>(this Parser<T> parser, Func<T, Parser<U>> selector, Func<T, U, V> projector)
    {
        return parser.Then(t => selector(t).Select(u => projector(t, u)));
    }
}
