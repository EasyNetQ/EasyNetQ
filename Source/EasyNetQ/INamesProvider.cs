namespace EasyNetQ
{
    public interface INamesProvider
    {
        string EasyNetQErrorQueue { get; }
        string ErrorExchangePrefix { get; }
        string RpcExchange { get; }
    }
}