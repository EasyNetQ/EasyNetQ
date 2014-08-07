using System;
using EasyNetQ.Producer;

namespace EasyNetQ.Rpc.ReuseQueue
{
    public class ReuseQueueAdvancedRpcFactory : IAdvancedRpcFactory
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        private readonly IEventBus _eventBus;

        public ReuseQueueAdvancedRpcFactory(IConnectionConfiguration configuration, IRpcHeaderKeys rpcHeaderKeys, IEventBus eventBus)
        {
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
            _eventBus = eventBus;
        }

        public IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus)
        {
            var responseQueueName = "rpc:" + Guid.NewGuid().ToString(); // TODO this should be changed to something more eatable
            return new ReuseQueueAdvancedClientRpc(advancedBus, _configuration, _rpcHeaderKeys, _eventBus, responseQueueName);
        }

        public IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus)
        {
            return new AdvancedServerRpc(advancedBus, _configuration, _rpcHeaderKeys);
        }
    }
}
