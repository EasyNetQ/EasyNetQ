namespace EasyNetQ.Sprache
{
    internal sealed class Success<T> : ISuccess<T>
    {
        readonly Input _remainder;
        readonly T _result;

        public Success(T result, Input remainder)
        {
            _result = result;
            _remainder = remainder;
        }

        public T Result => _result;

        public Input Remainder => _remainder;

        public override string ToString()
        {
            return string.Format("Successful parsing of {0}.", Result);
        }
    }
}
