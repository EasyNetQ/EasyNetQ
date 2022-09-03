namespace EasyNetQ.Sprache;

internal sealed class Success<T> : ISuccess<T>
{
    public Success(T result, Input remainder)
    {
        Result = result;
        Remainder = remainder;
    }

    public T Result { get; }

    public Input Remainder { get; }

    public override string ToString()
    {
        return $"Successful parsing of {Result}.";
    }
}
