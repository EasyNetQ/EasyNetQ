using System.Collections.Generic;

namespace EasyNetQ.Sprache
{
    internal interface IFailure<out T> : IResult<T>
    {
        string Message { get; }
        IEnumerable<string> Expectations { get; }
        Input FailedInput { get; }
    }
}
