using EasyNetQ.Producer;

namespace EasyNetQ.Rpc.FreshQueue
{
    class FreshQueueRpcFactory : IAdvancedRpcFactory
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IAdvancedPublishExchangeDeclareStrategy _advancedPublishExchangeDeclareStrategy;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;

        private IAdvancedClientRpc _client;
        private IAdvancedServerRpc _server;

        public FreshQueueRpcFactory(
            IConnectionConfiguration configuration,
            IAdvancedPublishExchangeDeclareStrategy advancedPublishExchangeDeclareStrategy,
            IRpcHeaderKeys rpcHeaderKeys)
        {
            _configuration = configuration;
            _advancedPublishExchangeDeclareStrategy = advancedPublishExchangeDeclareStrategy;
            _rpcHeaderKeys = rpcHeaderKeys;
        }

        public IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus)
        {
            return (_client = _client ?? new FreshQueueClientRpc(advancedBus,_configuration,_rpcHeaderKeys));
        }

        public IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus)
        {
            return (_server = _server ?? new FreshQueueServerRpc(advancedBus, _advancedPublishExchangeDeclareStrategy, _configuration, _rpcHeaderKeys));
        }
    }
}
