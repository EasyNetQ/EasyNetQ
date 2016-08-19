namespace Sprache
{
    public interface ISuccess<out T> : IResult<T>
    {
        T Result { get; }
        Input Remainder { get; }
    }
}