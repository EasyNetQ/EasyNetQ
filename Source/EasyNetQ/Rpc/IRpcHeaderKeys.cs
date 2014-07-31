namespace EasyNetQ.Rpc
{
    interface IRpcHeaderKeys
    {
        string IsFaultedKey { get; }
        string ExceptionMessageKey { get; }
    }
}