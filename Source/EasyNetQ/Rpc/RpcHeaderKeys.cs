namespace EasyNetQ.Rpc
{
    public class RpcHeaderKeys : IRpcHeaderKeys
    {
        public string IsFaultedKey { get { return "IsFaulted"; } }
        public string ExceptionMessageKey { get { return "ExceptionMessage"; } }
    }
}