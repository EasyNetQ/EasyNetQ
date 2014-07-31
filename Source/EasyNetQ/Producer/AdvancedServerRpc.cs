using System;
using System.Threading.Tasks;
using EasyNetQ.Consumer;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    class AdvancedServerRpc : IAdvancedServerRpc
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IAdvancedPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        private readonly IConnectionConfiguration configuration;
        private readonly IRpcHeaderKeys _rpcHeaderKeys;

        public AdvancedServerRpc(
            IAdvancedBus advancedBus,
            IAdvancedPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IConnectionConfiguration configuration,
            IRpcHeaderKeys rpcHeaderKeys)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(configuration, "configuration");

            this.advancedBus = advancedBus;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.configuration = configuration;
            _rpcHeaderKeys = rpcHeaderKeys;
        }

        public IDisposable Respond(IExchange requestExchange, IQueue queue, string topic, Func<SerializedMessage, Task<SerializedMessage>> handleRequest)
        {
            Preconditions.CheckNotNull(requestExchange, "requestExchange");
            Preconditions.CheckNotNull(queue, "queue");
            Preconditions.CheckNotNull(topic, "topic");
            Preconditions.CheckNotNull(handleRequest, "handleRequest");

            var expires = (int)TimeSpan.FromSeconds(configuration.Timeout).TotalMilliseconds;
            advancedBus.QueueDeclare(queue.Name, 
                passive: false, 
                durable: false, 
                exclusive: queue.IsExclusive,
                autoDelete: false, 
                expires: expires);

            advancedBus.Bind(requestExchange, queue, topic);

            var responseExchange = Exchange.GetDefault();
            return advancedBus.Consume(queue, (msgBytes, msgProp, messageRecievedInfo) => ExecuteResponder(responseExchange, handleRequest, new SerializedMessage(msgProp, msgBytes)));
        }
            
        private Task ExecuteResponder(IExchange responseExchange, Func<SerializedMessage, Task<SerializedMessage>> responder, SerializedMessage requestMessage) 
        {
            var tcs = new TaskCompletionSource<object>();

            responder(requestMessage).ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        if (task.Exception != null)
                        {
                            var responseMessage = new SerializedMessage(new MessageProperties(), new byte[] { });
                            responseMessage.Properties.Headers.Add(_rpcHeaderKeys.IsFaultedKey, true);
                            responseMessage.Properties.Headers.Add(_rpcHeaderKeys.ExceptionMessageKey, task.Exception.InnerException.Message);
                            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                            advancedBus.Publish(responseExchange, requestMessage.Properties.ReplyTo, false, false, responseMessage.Properties, requestMessage.Body);
                            tcs.SetException(task.Exception);
                        }
                    }
                    else
                    {
                        var responseMessage = task.Result;
                        responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                        advancedBus.Publish(responseExchange, requestMessage.Properties.ReplyTo, false, false, responseMessage.Properties, responseMessage.Body);
                        tcs.SetResult(null);
                    }
                });
            return tcs.Task;
        }
    }
}