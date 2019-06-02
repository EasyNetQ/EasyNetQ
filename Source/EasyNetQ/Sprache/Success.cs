namespace EasyNetQ.Sprache
{
    internal sealed class Success<T> : ISuccess<T>
    {
        public Success(T result, Input remainder)
        {
            Result = result;
            Remainder = remainder;
        }

        public T Result { get; private set; }

        public Input Remainder { get; private set; }

        public override string ToString()
        {
            return string.Format("Successful parsing of {0}.", Result);
        }
    }
}
