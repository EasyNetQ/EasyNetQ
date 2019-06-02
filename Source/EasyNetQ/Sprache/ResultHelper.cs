using System;

namespace EasyNetQ.Sprache
{
    internal static class ResultHelper
    {
        public static IResult<U> IfSuccess<T, U>(this IResult<T> result, Func<ISuccess<T>, IResult<U>> next)
        {
            if (result is ISuccess<T> s)
                return next(s);

            var f = (IFailure<T>)result;
            return new Failure<U>(f.FailedInput, () => f.Message, () => f.Expectations);
        }

        public static IResult<T> IfFailure<T>(this IResult<T> result, Func<IFailure<T>, IResult<T>> next)
        {
            if (result is ISuccess<T> s)
                return s;
            var f = (IFailure<T>)result;
            return next(f);
        }
    }
}
