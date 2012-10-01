using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sprache
{
    public delegate IResult<T> Parser<out T>(Input input);

    public static class ParserExtensions
    {
        public static IResult<T> TryParse<T>(this Parser<T> parser, string input)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            if (input == null) throw new ArgumentNullException("input");

            return parser(new Input(input));
        }

        public static T Parse<T>(this Parser<T> parser, string input)
        {
            if (parser == null) throw new ArgumentNullException("parser");
            if (input == null) throw new ArgumentNullException("input");

            var result = parser.TryParse(input);
            
            var success = result as ISuccess<T>;
            if (success != null)
                return success.Result;

            throw new ParseException(result.ToString());
        }
    }
}
