namespace Sprache
{
    sealed class Success<T> : ISuccess<T>
    {
        readonly Input _remainder;
        readonly T _result;

        public Success(T result, Input remainder)
        {
            _result = result;
            _remainder = remainder;
        }

        public T Result { get { return _result; } }

        public Input Remainder { get { return _remainder; } }

        public override string ToString()
        {
            return string.Format("Successful parsing of {0}.", Result);
        }
    }
}