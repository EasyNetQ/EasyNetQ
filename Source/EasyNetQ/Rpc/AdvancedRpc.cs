using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    public class AdvancedRpc : IRpc
    {
        private readonly IAdvancedClientRpc _clientRpc;
        private readonly IAdvancedServerRpc _serverRpc;
        private readonly IAdvancedBus _advancedBus;
        private readonly IConventions _conventions;
        private readonly IMessageSerializationStrategy _messageSerializationStrategy;
        private readonly IConnectionConfiguration _connectionConfiguration;

        public AdvancedRpc(IAdvancedBus advancedBus, IAdvancedRpcFactory advancedRpcFactory, IConventions conventions, IMessageSerializationStrategy messageSerializationStrategy, IConnectionConfiguration connectionConfiguration)
        {
            _clientRpc = advancedRpcFactory.CreateClientRpc(advancedBus);
            _serverRpc = advancedRpcFactory.CreateServerRpc(advancedBus);
            _advancedBus = advancedBus;
            _conventions = conventions;
            _messageSerializationStrategy = messageSerializationStrategy;
            _connectionConfiguration = connectionConfiguration;
        }

        public Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            var exchangeName = _conventions.RpcExchangeNamingConvention();
            var requestExchange = new Exchange(exchangeName);
            var requestRoutingKey = _conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
            var timeout = TimeSpan.FromSeconds(_connectionConfiguration.Timeout);

            var message = new Message<TRequest>(request);
            var serializedMessage = _messageSerializationStrategy.SerializeMessage(message);
            
            var response = _clientRpc.RequestAsync(requestExchange, requestRoutingKey, false, false, timeout, serializedMessage);
            return response.Then(sMsg => TaskHelpers.FromResult(((IMessage<TResponse>) _messageSerializationStrategy.DeserializeMessage(sMsg.Properties, sMsg.Body).Message).Body));
        }

        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            var handlerId = "";

            var exchange = _conventions.RpcExchangeNamingConvention();
            var queue = new Queue(_conventions.RpcRequestQueueNameConvention(typeof (TRequest), handlerId),false);
            var topic = _conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));

            return _serverRpc.Respond(exchange, queue.Name, topic, (message, info) => HandleRequest(responder)(message));
        }

        private Func<SerializedMessage, Task<SerializedMessage>> HandleRequest<TRequest,TResponse>(Func<TRequest, Task<TResponse>> handle) where TResponse : class
        {
            return serializedMessage => Task.Factory.StartNew(() =>
                {
                    var deserializedMessage = _messageSerializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);

                    return (IMessage<TRequest>)deserializedMessage.Message;
                })
                .Then(request => handle(request.Body))
                .Then(response => TaskHelpers.FromResult(_messageSerializationStrategy.SerializeMessage(new Message<TResponse>(response))));
        }
    }
}
