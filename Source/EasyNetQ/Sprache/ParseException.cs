using System;

namespace Sprache
{
    public class ParseException : Exception
    {
        public ParseException(string message)
            : base(message)
        {
        }
    }
}
