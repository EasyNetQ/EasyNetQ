using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Rpc
{
    class BetterRpc : IRpc
    {
        private readonly IAdvancedClientRpc _clientRpc;
        private readonly IAdvancedServerRpc _serverRpc;
        private readonly IConventions _conventions;
        private readonly IMessageSerializationStrategy _messageSerializationStrategy;

        public BetterRpc(IAdvancedClientRpc clientRpc, IAdvancedServerRpc serverRpc, IConventions conventions, IMessageSerializationStrategy messageSerializationStrategy)
        {
            _clientRpc = clientRpc;
            _serverRpc = serverRpc;
            _conventions = conventions;
            _messageSerializationStrategy = messageSerializationStrategy;
        }

        public Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class
        {
            var exchange = new Exchange(_conventions.RpcExchangeNamingConvention());
            var requestRoutingKey = _conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
            var message = new Message<TRequest>(request);

            var serializedMessage = _messageSerializationStrategy.SerializeMessage(message);

            var response = _clientRpc.RequestAsync(exchange, requestRoutingKey, false, false, () => _conventions.RpcReturnQueueNamingConvention(), serializedMessage);
            return response.ContinueWith(
                task => ((IMessage<TResponse>)_messageSerializationStrategy.DeserializeMessage(task.Result.Properties, task.Result.Body).Message).Body,
                TaskContinuationOptions.NotOnFaulted);
        }

        //TODO add handlerId
        public IDisposable Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class
        {
            var handlerId = "";

            var exchange = new Exchange(_conventions.RpcExchangeNamingConvention());
            var queue = new Queue(_conventions.RpcRequestQueueNameConvention(typeof (TRequest), handlerId),false);
            var topic = _conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));

            return _serverRpc.Respond(exchange, queue, topic, HandleRequest(responder));
        }

        private Func<SerializedMessage, Task<SerializedMessage>> HandleRequest<TRequest,TResponse>(Func<TRequest, Task<TResponse>> handle) where TResponse : class
        {
            return serializedMessage =>
                {
                    var deserializedMessage = _messageSerializationStrategy.DeserializeMessage(serializedMessage.Properties, serializedMessage.Body);
                    var request = (IMessage<TRequest>) deserializedMessage.Message;
                    var responseTask = handle(request.Body);
                    return responseTask.ContinueWith(
                        task => _messageSerializationStrategy.SerializeMessage(new Message<TResponse>(task.Result)),
                        TaskContinuationOptions.NotOnFaulted);
                };
        }
    }
}