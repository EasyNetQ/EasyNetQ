using System;

namespace EasyNetQ
{
    public class EasyNetQException : Exception
    {
        public EasyNetQException() { }
        public EasyNetQException(string message) : base(message) { }
        public EasyNetQException(string format, params object[] args) : base(string.Format(format, args)) { }
        public EasyNetQException(string message, Exception inner) : base(message, inner) { }
    }

    public class EasyNetQResponderException : EasyNetQException
    {
        public EasyNetQResponderException() { }
        public EasyNetQResponderException(string message) : base(message) { }
        public EasyNetQResponderException(string format, params object[] args) : base(format, args) { }
        public EasyNetQResponderException(string message, Exception inner) : base(message, inner) { }
    }
}
