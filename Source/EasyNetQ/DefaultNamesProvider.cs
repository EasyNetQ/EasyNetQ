namespace EasyNetQ
{
    public class DefaultNamesProvider : INamesProvider
    {
        public string EasyNetQErrorQueue
        {
            get { return "EasyNetQ_Default_Error_Queue"; }
        }

        public string ErrorExchangePrefix
        {
            get { return "ErrorExchange_"; }
        }

        public string RpcExchange
        {
            get { return "easy_net_q_rpc"; }
        }
    }
}