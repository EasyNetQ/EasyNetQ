﻿using System;

namespace EasyNetQ
{
#if !NET_CORE
#endif
    public class EasyNetQException : Exception
    {
        public EasyNetQException() {}
        public EasyNetQException(string message) : base(message) {}
        public EasyNetQException(string format, params string[] args) : base(string.Format(format, args)) {}
        public EasyNetQException(string message, Exception inner) : base(message, inner) {}

#if !NET_CORE
#endif
    }

#if !NET_CORE
#endif
    public class EasyNetQInvalidMessageTypeException : EasyNetQException
    {
        public EasyNetQInvalidMessageTypeException() {}
        public EasyNetQInvalidMessageTypeException(string message) : base(message) {}
        public EasyNetQInvalidMessageTypeException(string format, params string[] args) : base(format, args) {}
        public EasyNetQInvalidMessageTypeException(string message, Exception inner) : base(message, inner) {}
#if !NET_CORE
#endif
    }

#if !NET_CORE
#endif
    public class EasyNetQResponderException : EasyNetQException
    {
        public EasyNetQResponderException() { }
        public EasyNetQResponderException(string message) : base(message) { }
        public EasyNetQResponderException(string format, params string[] args) : base(format, args) { }
        public EasyNetQResponderException(string message, Exception inner) : base(message, inner) { }
#if !NET_CORE
#endif
    }
}