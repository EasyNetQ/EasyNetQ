﻿using System;
using System.Linq;
using System.Threading.Tasks;
using EasyNetQ.Rpc.FreshQueue;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    class AdvancedServerRpc : IAdvancedServerRpc
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IConnectionConfiguration configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;

        public AdvancedServerRpc(
            IAdvancedBus advancedBus,
            IConnectionConfiguration configuration,
            IRpcHeaderKeys rpcHeaderKeys)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.advancedBus = advancedBus;
            this.configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }

        public IDisposable Respond(IExchange requestExchange, string queueName, string topic, Func<SerializedMessage, Task<SerializedMessage>> handleRequest)
        {
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(queueName, "queueName");
            Preconditions.CheckNotNull(topic, "topic");
            Preconditions.CheckNotNull(handleRequest, "handleRequest");

            var expires = (int)TimeSpan.FromSeconds(configuration.Timeout).TotalMilliseconds;
            var queue = advancedBus.QueueDeclare(queueName, 
                passive: false, 
                durable: false, 
                exclusive: false,
                autoDelete: true, 
                expires: expires);

            advancedBus.Bind(requestExchange, queue, topic);

            var responseExchange = Exchange.GetDefault();
            return advancedBus.Consume(queue, (msgBytes, msgProp, messageRecievedInfo) => ExecuteResponder(responseExchange, handleRequest, new SerializedMessage(msgProp, msgBytes)));
        }
            
        private Task ExecuteResponder(IExchange responseExchange, Func<SerializedMessage, Task<SerializedMessage>> responder, SerializedMessage requestMessage)
        {
            return responder(requestMessage)
                .ContinueWith(RpcHelpers.MaybeAddExceptionToHeaders(_rpcHeaderKeys, requestMessage))
                .Then(uhInfo =>
                    {
                        var sm = uhInfo.Response;
                        sm.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
                        advancedBus.Publish(responseExchange, requestMessage.Properties.ReplyTo, false, false, sm.Properties, sm.Body);
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