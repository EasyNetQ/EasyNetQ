using System;

namespace EasyNetQ.Sprache
{
    internal class ParseException : Exception
    {
        public ParseException(string message)
            : base(message)
        {
        }
    }
}
