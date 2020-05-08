using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Sprache
{
    /// <summary>
    /// Parsers and combinators.
    /// </summary>
    internal static class Parse
    {
        public static readonly Parser<char> AnyChar = Char(c => true, "any character");
        public static readonly Parser<char> WhiteSpace = Char(char.IsWhiteSpace, "whitespace");
        public static readonly Parser<char> Digit = Char(char.IsDigit, "digit");
        public static readonly Parser<char> Letter = Char(char.IsLetter, "letter");
        public static readonly Parser<char> LetterOrDigit = Char(char.IsLetterOrDigit, "letter or digit");
        public static readonly Parser<char> Lower = Char(char.IsLower, "lowercase letter");
        public static readonly Parser<char> Upper = Char(char.IsUpper, "upper");
        public static readonly Parser<char> Numeric = Char(char.IsNumber, "numeric character");

        public static readonly Parser<string> Number = Numeric.AtLeastOnce().Text();

        public static readonly Parser<string> Decimal =
            from integral in Number
            from fraction in Char('.').Then(point => Number.Select(n => "." + n)).XOr(Return(""))
            select integral + fraction;

        /// <summary>
        /// TryParse a single character matching 'predicate'
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="description"></param>
        /// <returns></returns>
        public static Parser<char> Char(Predicate<char> predicate, string description)
        {
            Preconditions.CheckNotNull(predicate, "predicate");
            Preconditions.CheckNotNull(description, "description");

            return i =>
            {
                if (!i.AtEnd)
                {
                    if (predicate(i.Current))
                        return new Success<char>(i.Current, i.Advance());

                    return new Failure<char>(i,
                        () => string.Format("unexpected '{0}'", i.Current),
                        () => new[] { description });
                }

                return new Failure<char>(i,
                    () => "Unexpected end of input reached",
                    () => new[] { description });
            };
        }

        /// <summary>
        /// Parse a single character except those matching <paramref name="predicate"/>.
        /// </summary>
        /// <param name="predicate">Characters not to match.</param>
        /// <param name="description">Description of characters that don't match.</param>
        /// <returns>A parser for characters except those matching <paramref name="predicate"/>.</returns>
        public static Parser<char> CharExcept(Predicate<char> predicate, string description)
        {
            return Char(c => !predicate(c), "any character except " + description);
        }

        /// <summary>
        /// Parse a single character c.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Parser<char> Char(char c)
        {
            return Char(ch => c == ch, c.ToString());
        }

        /// <summary>
        /// Parse a single character c.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Parser<char> CharCaseInsensitive(char c)
        {
            return Char(ch => char.ToLower(c) == char.ToLower(ch), c.ToString());
        }

        /// <summary>
        /// Parse a single character except c.
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Parser<char> CharExcept(char c)
        {
            return CharExcept(ch => c == ch, c.ToString());
        }

        /// <summary>
        /// Parse a string of characters.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<char>> String(string s)
        {
            Preconditions.CheckNotNull(s, "s");

            return s
                .Select(Char)
                .Aggregate(Return(Enumerable.Empty<char>()),
                    (a, p) => a.Concat(p.Once()))
                .Named(s);
        }

        /// <summary>
        /// Parse a string of characters.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<char>> CaseInsensitiveString(string s)
        {
            Preconditions.CheckNotNull(s, "s");

            return s
                .Select(CharCaseInsensitive)
                .Aggregate(Return(Enumerable.Empty<char>()),
                    (a, p) => a.Concat(p.Once()))
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
            Preconditions.CheckNotNull(first, "first");
            Preconditions.CheckNotNull(second, "second");

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
            Preconditions.CheckNotNull(parser, "parser");

            return i =>
            {
                var remainder = i;
                var result = new List<T>();
                var r = parser(i);
                while (r is ISuccess<T>)
                {
                    var s = r as ISuccess<T>;
                    if (remainder == s.Remainder)
                        break;

                    result.Add(s.Result);
                    remainder = s.Remainder;
                    r = parser(remainder);
                }

                return new Success<IEnumerable<T>>(result, remainder);
            };
        }

        /// <summary>
        /// Parse a stream of elements. If any element is partially parsed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        /// <remarks>Implemented imperatively to decrease stack usage.</remarks>
        public static Parser<IEnumerable<T>> XMany<T>(this Parser<T> parser)
        {
            Preconditions.CheckNotNull(parser, "parser");

            return parser.Many().Then(m => parser.Once().XOr(Return(m)));
        }

        /// <summary>
        /// TryParse a stream of elements with at least one item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> AtLeastOnce<T>(this Parser<T> parser)
        {
            Preconditions.CheckNotNull(parser, "parser");

            return parser.Once().Then(t1 => parser.Many().Select(ts => t1.Concat(ts)));
        }

        /// <summary>
        /// Parse end-of-input.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static Parser<T> End<T>(this Parser<T> parser)
        {
            Preconditions.CheckNotNull(parser, "parser");

            return i => parser(i).IfSuccess(s =>
                s.Remainder.AtEnd
                    ? (IResult<T>)s
                    : new Failure<T>(
                        s.Remainder,
                        () => string.Format("unexpected '{0}'", s.Remainder.Current),
                        () => new[] { "end of input" }));
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
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(convert, "convert");

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
            Preconditions.CheckNotNull(parser, "parser");

            return from leading in WhiteSpace.Many()
                from item in parser
                from trailing in WhiteSpace.Many()
                select item;
        }

        /// <summary>
        /// Refer to another parser indirectly. This allows circular compile-time dependency between parsers.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reference"></param>
        /// <returns></returns>
        public static Parser<T> Ref<T>(Func<Parser<T>> reference)
        {
            Preconditions.CheckNotNull(reference, "reference");

            Parser<T> p = null;

            return i =>
            {
                if (p == null)
                    p = reference();

                if (i.Memos.ContainsKey(p))
                    throw new ParseException(i.Memos[p].ToString());

                i.Memos[p] = new Failure<T>(i,
                    () => "Left recursion in the grammar.",
                    () => new string[0]);
                var result = p(i);
                i.Memos[p] = result;
                return result;
            };
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
            Preconditions.CheckNotNull(first, "first");
            Preconditions.CheckNotNull(second, "second");

            return i =>
            {
                var fr = first(i);
                if (fr is IFailure<T> ff)
                    return second(i).IfFailure(sf => new Failure<T>(
                        ff.FailedInput,
                        () => ff.Message,
                        () => ff.Expectations.Union(sf.Expectations)));

                var fs = (ISuccess<T>)fr;
                if (fs.Remainder == i)
                    return second(i).IfFailure(sf => fs);

                return fs;
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
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(name, "name");

            return i => parser(i).IfFailure(f => f.FailedInput == i ? new Failure<T>(f.FailedInput, () => f.Message, () => new[] { name }) : f);
        }

        /// <summary>
        /// Parse first, if it succeeds, return first, otherwise try second.
        /// Assumes that the first parsed character will determine the parser chosen (see Try).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="first"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        public static Parser<T> XOr<T>(this Parser<T> first, Parser<T> second)
        {
            Preconditions.CheckNotNull(first, "first");
            Preconditions.CheckNotNull(second, "second");

            return i =>
            {
                var fr = first(i);
                if (fr is IFailure<T> ff)
                {
                    if (ff.FailedInput != i)
                        return ff;

                    return second(i).IfFailure(sf => new Failure<T>(
                        ff.FailedInput,
                        () => ff.Message,
                        () => ff.Expectations.Union(sf.Expectations)));
                }

                var fs = (ISuccess<T>)fr;
                if (fs.Remainder == i)
                    return second(i).IfFailure(sf => fs);

                return fs;
            };
        }

        /// <summary>
        /// Parse a stream of elements containing only one item.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> Once<T>(this Parser<T> parser)
        {
            Preconditions.CheckNotNull(parser, "parser");

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
            Preconditions.CheckNotNull(first, "first");
            Preconditions.CheckNotNull(second, "second");

            return first.Then(f => second.Select(s => f.Concat(s)));
        }

        /// <summary>
        /// Succeed immediately and return value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Parser<T> Return<T>(T value)
        {
            return i => new Success<T>(value, i);
        }

        /// <summary>
        /// Version of Return with simpler inline syntax.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Parser<U> Return<T, U>(this Parser<T> parser, U value)
        {
            Preconditions.CheckNotNull(parser, "parser");
            return parser.Select(t => value);
        }

        /// <summary>
        /// Attempt parsing only if the <paramref name="except"/> parser fails.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="except"></param>
        /// <returns></returns>
        public static Parser<T> Except<T, U>(this Parser<T> parser, Parser<U> except)
        {
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(except, "except");

            // Could be more like: except.Then(s => s.Fail("..")).XOr(parser)
            return i =>
            {
                var r = except(i);
                if (r is ISuccess<U>)
                    return new Failure<T>(i, () => "Excepted parser succeeded.", () => new[] { "other than the excepted input" });
                return parser(i);
            };
        }

        /// <summary>
        /// Parse a sequence of items until a terminator is reached.
        /// Returns the sequence, discarding the terminator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="parser"></param>
        /// <param name="until"></param>
        /// <returns></returns>
        public static Parser<IEnumerable<T>> Until<T, U>(this Parser<T> parser, Parser<U> until)
        {
            return parser.Except(until).Many().Then(r => until.Return(r));
        }

        /// <summary>
        /// Succeed if the parsed value matches predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parser"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static Parser<T> Where<T>(this Parser<T> parser, Func<T, bool> predicate)
        {
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(predicate, "predicate");

            return i => parser(i).IfSuccess(s =>
                predicate(s.Result)
                    ? (IResult<T>)s
                    : new Failure<T>(i,
                        () => string.Format("Unexpected {0}.", s.Result),
                        () => new string[0]));
        }

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
        public static Parser<V> SelectMany<T, U, V>(
            this Parser<T> parser,
            Func<T, Parser<U>> selector,
            Func<T, U, V> projector)
        {
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(selector, "selector");
            Preconditions.CheckNotNull(projector, "projector");

            return parser.Then(t => selector(t).Select(u => projector(t, u)));
        }

        /// <summary>
        /// Chain a left-associative operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOp"></typeparam>
        /// <param name="op"></param>
        /// <param name="operand"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static Parser<T> ChainOperator<T, TOp>(
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            Preconditions.CheckNotNull(op, "op");
            Preconditions.CheckNotNull(operand, "operand");
            Preconditions.CheckNotNull(apply, "apply");
            return operand.Then(first => ChainOperatorRest(first, op, operand, apply));
        }

        private static Parser<T> ChainOperatorRest<T, TOp>(
            T firstOperand,
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            Preconditions.CheckNotNull(op, "op");
            Preconditions.CheckNotNull(operand, "operand");
            Preconditions.CheckNotNull(apply, "apply");
            return op.Then(opvalue =>
                    operand.Then(operandValue =>
                        ChainOperatorRest(apply(opvalue, firstOperand, operandValue), op, operand, apply)))
                .Or(Return(firstOperand));
        }

        /// <summary>
        /// Chain a right-associative operator.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TOp"></typeparam>
        /// <param name="op"></param>
        /// <param name="operand"></param>
        /// <param name="apply"></param>
        /// <returns></returns>
        public static Parser<T> ChainRightOperator<T, TOp>(
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            Preconditions.CheckNotNull(op, "op");
            Preconditions.CheckNotNull(operand, "operand");
            Preconditions.CheckNotNull(apply, "apply");
            return operand.Then(first => ChainRightOperatorRest(first, op, operand, apply));
        }

        private static Parser<T> ChainRightOperatorRest<T, TOp>(
            T lastOperand,
            Parser<TOp> op,
            Parser<T> operand,
            Func<TOp, T, T, T> apply)
        {
            Preconditions.CheckNotNull(op, "op");
            Preconditions.CheckNotNull(operand, "operand");
            Preconditions.CheckNotNull(apply, "apply");
            return op.Then(opvalue =>
                    operand.Then(operandValue =>
                        ChainRightOperatorRest(operandValue, op, operand, apply)).Then(r =>
                        Return(apply(opvalue, lastOperand, r))))
                .Or(Return(lastOperand));
        }
    }
}
