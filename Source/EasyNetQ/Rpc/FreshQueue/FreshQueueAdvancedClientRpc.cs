using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc.FreshQueue
{
    class FreshQueueAdvancedClientRpc : IAdvancedClientRpc
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        
        public FreshQueueAdvancedClientRpc(IAdvancedBus advancedBus, IConnectionConfiguration configuration, IRpcHeaderKeys rpcHeaderKeys)
        {
            _advancedBus = advancedBus;
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }
        public Task<SerializedMessage> RequestAsync(string requestExchange, string requestRoutingKey, bool mandatory, bool immediate, TimeSpan timeout, SerializedMessage request)
        {
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(requestRoutingKey, "requestRoutingKey");
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();
            var responseQueueName = "rpc:"+correlationId;

            // assume that the server declares the exchange
            var exchange = new Exchange(requestExchange); 

            var queue = _advancedBus.QueueDeclare(
                responseQueueName,
                passive: false,
                durable: false,
                expires: (int) TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds,
                exclusive: true,
                autoDelete: true);

            //the response is published to the default exchange with the queue name as routingkey. So no need to bind to exchange
            var continuation = _advancedBus.ConsumeSingle(new Queue(queue.Name, queue.IsExclusive), timeout);

            RpcHelpers.PublishRequest(_advancedBus, exchange, request, requestRoutingKey, responseQueueName, correlationId, timeout);
            return continuation
                .Then(mcc => TaskHelpers.FromResult(new SerializedMessage(mcc.Properties, mcc.Message)))
                .Then(sm => RpcHelpers.ExtractExceptionFromHeadersAndPropagateToTask(_rpcHeaderKeys, sm));
        }

        
    }
}
