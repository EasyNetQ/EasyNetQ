namespace EasyNetQ.Rpc
{
    class RpcHeaderKeys : IRpcHeaderKeys
    {
        public string IsFaultedKey { get { return "IsFaulted"; } }
        public string ExceptionMessageKey { get { return "ExceptionMessage"; } }
    }
}