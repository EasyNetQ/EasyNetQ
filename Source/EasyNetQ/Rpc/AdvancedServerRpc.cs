using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    class AdvancedServerRpc : IAdvancedServerRpc
    {
        private readonly IAdvancedBus _advancedBus;
        private readonly IConnectionConfiguration _configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;

        public AdvancedServerRpc(
            IAdvancedBus advancedBus,
            IConnectionConfiguration configuration,
            IRpcHeaderKeys rpcHeaderKeys)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this._advancedBus = advancedBus;
            this._configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }

        public IDisposable Respond(string requestExchange, string queueName, string topic, Func<SerializedMessage, MessageReceivedInfo, Task<SerializedMessage>> handleRequest)
        {
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(queueName, "queueName");
            Preconditions.CheckNotNull(topic, "topic");
            Preconditions.CheckNotNull(handleRequest, "handleRequest");

            var expires = (int)TimeSpan.FromSeconds(_configuration.Timeout).TotalMilliseconds;

            var exchange = _advancedBus.ExchangeDeclare(requestExchange, ExchangeType.Topic);

            var queue = _advancedBus.QueueDeclare(queueName, 
                passive: false, 
                durable: false, 
                exclusive: false,
                autoDelete: true, 
                expires: expires);

            _advancedBus.Bind(exchange, queue, topic);

            var responseExchange = Exchange.GetDefault();
            return _advancedBus.Consume(queue, (msgBytes, msgProp, messageRecievedInfo) => ExecuteResponder(responseExchange, handleRequest, new SerializedMessage(msgProp, msgBytes), messageRecievedInfo));
        }

        private Task ExecuteResponder(IExchange responseExchange, Func<SerializedMessage, MessageReceivedInfo, Task<SerializedMessage>> responder, SerializedMessage requestMessage, MessageReceivedInfo messageRecievedInfo)
        {
            return responder(requestMessage, messageRecievedInfo)
                .ContinueWith(RpcHelpers.MaybeAddExceptionToHeaders(_rpcHeaderKeys, requestMessage))
                .Then(uhInfo =>
                    {
                        var sm = uhInfo.Response;
                        sm.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
                        _advancedBus.Publish(responseExchange, requestMessage.Properties.ReplyTo, false, false, sm.Properties, sm.Body);
                        return TaskHelpers.FromResult(uhInfo);
                    })
                .Then(uhInfo => 
                    {
                        if (uhInfo.IsFailed())
                        {
                            throw new EasyNetQResponderException("MessageHandler Failed", uhInfo.Exception);
                        }
                        return TaskHelpers.FromResult(0);
                    });
        }
    }
}