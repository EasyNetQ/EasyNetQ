using System;
using EasyNetQ.Producer;

namespace EasyNetQ.Rpc.ReuseQueue
{
    public class ReuseQueueAdvancedRpcFactory : IAdvancedRpcFactory
    {
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        private readonly IEventBus _eventBus;
        private readonly IAdvancedPublishExchangeDeclareStrategy _exchangeDeclareStrategy;

        public ReuseQueueAdvancedRpcFactory(IConnectionConfiguration configuration, IRpcHeaderKeys rpcHeaderKeys, IEventBus eventBus, IAdvancedPublishExchangeDeclareStrategy exchangeDeclareStrategy)
        {
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
            _eventBus = eventBus;
            _exchangeDeclareStrategy = exchangeDeclareStrategy;
        }

        public IAdvancedClientRpc CreateClientRpc(IAdvancedBus advancedBus)
        {
            var responseQueueName = "rpc:" + Guid.NewGuid().ToString(); // TODO this should be changed to something more eatable
            return new ReuseQueueAdvancedClientRpc(advancedBus, _configuration, _rpcHeaderKeys, _eventBus, _exchangeDeclareStrategy, responseQueueName);
        }

        public IAdvancedServerRpc CreateServerRpc(IAdvancedBus advancedBus)
        {
            return new AdvancedServerRpc(advancedBus, _configuration, _rpcHeaderKeys);
        }
    }
}