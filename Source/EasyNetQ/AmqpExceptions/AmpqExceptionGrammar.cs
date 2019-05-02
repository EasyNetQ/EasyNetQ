using System.Collections.Generic;
using EasyNetQ.ConnectionString;
using EasyNetQ.Sprache;

namespace EasyNetQ.AmqpExceptions
{
    public static class AmqpExceptionGrammar
    {
        private static readonly Parser<ushort> Number = Parse.Number.Select(ushort.Parse);

        private static readonly Parser<IAmqpExceptionElement> codeElement = MakeIntegerElementParser<AmqpExceptionCodeElement>("code");
        private static readonly Parser<IAmqpExceptionElement> classIdElement = MakeIntegerElementParser<AmqpExceptionClassIdElement>("classId");
        private static readonly Parser<IAmqpExceptionElement> methodIdElement = MakeIntegerElementParser<AmqpExceptionMethodIdElement>("methodId");

        private static readonly Parser<IAmqpExceptionElement> keyValueElement =
            from key in Parse.CharExcept(c => c == '=' || c == ',', "").Many().Text().Token()
            from eq in Parse.Char('=')
            from value in Parse.CharExcept(',').Many().Text()
            select new AmqpExceptionKeyValueElement(key, value);

        private static readonly Parser<IAmqpExceptionElement> textElement =
            from text in Parse.CharExcept(',').Many().Text()
            select new TextElement(text);

        private static readonly Parser<IAmqpExceptionElement> element = methodIdElement
            .Or(classIdElement)
            .Or(codeElement)
            .Or(keyValueElement)
            .Or(textElement);

        private static readonly Parser<IEnumerable<IAmqpExceptionElement>> elements = element.ListDelimitedBy(',');

        private static readonly Parser<AmqpExceptionPreface> preface =
            from text in Parse.CharExcept(':').Many().Text()
            from colon in Parse.Char(':')
            select new AmqpExceptionPreface(text);

        private static readonly Parser<AmqpException> exception =
            from p in preface
            from e in elements
            select new AmqpException(p, new List<IAmqpExceptionElement>(e));

        private static Parser<T> MakeIntegerElementParser<T>(string key) where T : AmqpExceptionIntegerValueElement, new()
        {
            return from k in Parse.CaseInsensitiveString(key).Token()
                from eq in Parse.Char('=')
                from value in Number
                select new T {Value = value};
        }

        public static AmqpException ParseExceptionString(string exceptionMessage)
        {
            return exception.Parse(exceptionMessage);
        }
    }
}
