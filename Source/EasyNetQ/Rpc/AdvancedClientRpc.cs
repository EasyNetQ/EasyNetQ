using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    class AdvancedClientRpc : IAdvancedClientRpc
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;
        
        public AdvancedClientRpc(IAdvancedBus advancedBus, IConnectionConfiguration configuration, IRpcHeaderKeys rpcHeaderKeys)
        {
            _advancedBus = advancedBus;
            _configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }
        public Task<SerializedMessage> RequestAsync(IExchange requestExchange, string requestRoutingKey, bool mandatory, bool immediate, TimeSpan timeout, SerializedMessage request)
        {
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(requestRoutingKey, "requestRoutingKey");
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();
            var responseQueueName = "rpc:"+correlationId;

            var queue = _advancedBus.QueueDeclare(
                responseQueueName,
                passive: false,
                durable: false,
                expires: (int) TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds,
                exclusive: true,
                autoDelete: true);

            //the response is published to the default exchange with the queue name as routingkey. So no need to bind to exchange
            var continuation = _advancedBus.ConsumeSingle(new Queue(queue.Name, queue.IsExclusive), timeout);
            
            PublishRequest(requestExchange, request, requestRoutingKey, responseQueueName, correlationId);
            return continuation
                .Then(ExtractExceptionsFromHeaders)
                .Then(mcc => TaskHelpers.FromResult(new SerializedMessage(mcc.Properties, mcc.Message)));
        }

        private void PublishRequest(IExchange requestExchange, SerializedMessage request, string requestRoutingKey, string responseQueueName, Guid correlationId)
        {
            request.Properties.ReplyTo = responseQueueName;
            request.Properties.CorrelationId = correlationId.ToString();
            request.Properties.Expiration = TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds.ToString();

            //TODO write a specific RPC publisher that handles BasicReturn. Then we can set immediate+mandatory to true and react accordingly (now it will time out)
            _advancedBus.Publish(requestExchange, requestRoutingKey, false, false, request.Properties, request.Body);
        }

        private Task<MessageConsumeContext> ExtractExceptionsFromHeaders(MessageConsumeContext mcc)
        {
            var isFaulted = false;
            var exceptionMessage = "The exception message has not been specified.";
            if (mcc.Properties.HeadersPresent)
            {
                if (mcc.Properties.Headers.ContainsKey(_rpcHeaderKeys.IsFaultedKey))
                {
                    isFaulted = Convert.ToBoolean(mcc.Properties.Headers[_rpcHeaderKeys.IsFaultedKey]);
                }
                if (mcc.Properties.Headers.ContainsKey(_rpcHeaderKeys.ExceptionMessageKey))
                {
                    exceptionMessage = Encoding.UTF8.GetString((byte[]) mcc.Properties.Headers[_rpcHeaderKeys.ExceptionMessageKey]);
                }
            }
            return isFaulted ? TaskHelpers.FromException<MessageConsumeContext>(new EasyNetQResponderException(exceptionMessage)) : TaskHelpers.FromResult(mcc);
        }
    }
}
