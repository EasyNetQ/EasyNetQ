namespace EasyNetQ.Tests
{
    public class MockCustomNamesProvider : INamesProvider
    {
        public string EasyNetQErrorQueue {
            get { return "CustomEasyNetQErrorQueueName"; }
        }

        public string ErrorExchangePrefix
        {
            get { return "CustomErrorExchangePrefixName_"; }
        }

        public string RpcExchange
        {
            get { return "CustomRpcExchangeName"; }
        }
    }
}