namespace EasyNetQ.Rpc
{
    public interface IRpcHeaderKeys
    {
        string IsFaultedKey { get; }
        string ExceptionMessageKey { get; }
    }
}