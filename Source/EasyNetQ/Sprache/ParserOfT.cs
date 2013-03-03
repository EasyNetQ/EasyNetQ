using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ;

namespace Sprache
{
    public delegate IResult<T> Parser<out T>(Input input);

    public static class ParserExtensions
    {
        public static IResult<T> TryParse<T>(this Parser<T> parser, string input)
        {
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(input, "input");

            return parser(new Input(input));
        }

        public static T Parse<T>(this Parser<T> parser, string input)
        {
            Preconditions.CheckNotNull(parser, "parser");
            Preconditions.CheckNotNull(input, "input");

            var result = parser.TryParse(input);
            
            var success = result as ISuccess<T>;
            if (success != null)
                return success.Result;

            throw new ParseException(result.ToString());
        }
    }
}
