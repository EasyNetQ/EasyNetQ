using System;
using System.Collections.Generic;
using System.Linq;

namespace EasyNetQ.Sprache
{
    internal sealed class Failure<T> : IFailure<T>
    {
        readonly Func<IEnumerable<string>> _expectations;
        readonly Input _input;
        readonly Func<string> _message;

        public Failure(Input input, Func<string> message, Func<IEnumerable<string>> expectations)
        {
            _input = input;
            _message = message;
            _expectations = expectations;
        }

        public string Message => _message();

        public IEnumerable<string> Expectations => _expectations();

        public Input FailedInput => _input;

        public override string ToString()
        {
            var expMsg = "";

            if (Expectations.Any())
                expMsg = " expected " + Expectations.Aggregate((e1, e2) => e1 + " or " + e2);

            return string.Format("Parsing failure: {0};{1} ({2}).", Message, expMsg, FailedInput);
        }
    }
}
