using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class Rpc : IRpc
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IEventBus eventBus;
        private readonly IConventions conventions;
        private readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;

        public Rpc(IAdvancedBus advancedBus, IEventBus eventBus, IConventions conventions, IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");

            this.advancedBus = advancedBus;
            this.eventBus = eventBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
        }

        public Task<TResponse> Request<TRequest, TResponse>(TRequest request) 
            where TRequest : class 
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();

            var result = SubscribeToResponse<TRequest, TResponse>(correlationId);
            RequestPublish(request, result.Item2, correlationId);

            return result.Item1;
        }

        private Tuple<Task<TResponse>, string> SubscribeToResponse<TRequest, TResponse>(Guid correlationId)
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<TResponse>();

            responseActions.TryAdd(correlationId.ToString(), new ResponseAction
                {
                    OnSuccess = message => tcs.SetResult(((Message<TResponse>)message).Body)
                });

            var rpcKey = new RpcKey {Request = typeof (TRequest), Response = typeof (TResponse)};

            rpcKeys.AddOrUpdate(rpcKey,
                key =>
                    {
                        var queue = advancedBus.QueueDeclare(
                            conventions.RpcReturnQueueNamingConvention(),
                            passive: false,
                            durable: false,
                            exclusive: true,
                            autoDelete: true).SetAsSingleUse();

                        advancedBus.Consume<TResponse>(queue, (message, messageRecievedInfo) => Task.Factory.StartNew(() =>
                            {
                                if(responseActions.ContainsKey(message.Properties.CorrelationId))
                                {
                                    responseActions[message.Properties.CorrelationId].OnSuccess(message);
                                }
                            }));

                        return queue.Name;
                    },
                (key, queueName) => queueName);

            return new Tuple<Task<TResponse>, string>(tcs.Task, rpcKeys[rpcKey]);
        }

        private readonly ConcurrentDictionary<RpcKey, string> rpcKeys = new ConcurrentDictionary<RpcKey, string>();
        private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new ConcurrentDictionary<string, ResponseAction>();

        private struct RpcKey
        {
            public Type Request;
            public Type Response;
        }

        private class ResponseAction
        {
            public Action<object> OnSuccess { get; set; }
        }

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName, Guid correlationId) where TRequest : class
        {
            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
            var exchange = publishExchangeDeclareStrategy.DeclareExchange(
                advancedBus, conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);

            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;
            requestMessage.Properties.CorrelationId = correlationId.ToString();

            advancedBus.Publish(exchange, routingKey, false, false, requestMessage);
        }

        public void Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder) 
            where TRequest : class 
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");

            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));

            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);
            var queue = advancedBus.QueueDeclare(routingKey);
            advancedBus.Bind(exchange, queue, routingKey);

            advancedBus.Consume<TRequest>(queue, (requestMessage, messageRecievedInfo) =>
                {
                    var tcs = new TaskCompletionSource<object>();

                    responder(requestMessage.Body).ContinueWith(task =>
                        {
                            if (task.IsFaulted)
                            {
                                if (task.Exception != null)
                                {
                                    tcs.SetException(task.Exception);
                                }
                            }
                            else
                            {
                                var responseMessage = new Message<TResponse>(task.Result);
                                responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;

                                advancedBus.Publish(Exchange.GetDefault(), requestMessage.Properties.ReplyTo, false, false, responseMessage);
                                tcs.SetResult(null);
                            }
                        });

                    return tcs.Task;
                });
        }
    }
}