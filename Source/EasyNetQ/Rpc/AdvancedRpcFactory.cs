using System;
using EasyNetQ.Producer;

namespace EasyNetQ.Rpc
{
    class AdvancedRpcFactory : IAdvancedRpcFactory
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;

        private IAdvancedClientRpc _client;
        private IAdvancedServerRpc _server;

        public AdvancedRpcFactory(
            IConnectionConfiguration configuration,
            IRpcHeaderKeys rpcHeaderKeys)
        {
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }

        public IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus)
        {
            return (_client = _client ?? new AdvancedClientRpc(advancedBus,_configuration,_rpcHeaderKeys));
        }

        public IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus)
        {
            return (_server = _server ?? new AdvancedServerRpc(advancedBus, _configuration, _rpcHeaderKeys));
        }
    }
}
