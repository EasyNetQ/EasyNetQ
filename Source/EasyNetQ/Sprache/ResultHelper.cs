using System;

namespace EasyNetQ.Sprache
{
    internal static class ResultHelper
    {
        public static IResult<U> IfSuccess<T, U>(this IResult<T> result, Func<ISuccess<T>, IResult<U>> next)
        {
            var s = result as ISuccess<T>;
            if (s != null)
                return next(s);

            var f = (IFailure<T>) result;
            return new Failure<U>(f.FailedInput, () => f.Message, () => f.Expectations);
        }

        public static IResult<T> IfFailure<T>(this IResult<T> result, Func<IFailure<T>, IResult<T>> next)
        {
            var s = result as ISuccess<T>;
            if (s != null)
                return s;
            var f = (IFailure<T>) result;
            return next(f);
        }
    }
}
