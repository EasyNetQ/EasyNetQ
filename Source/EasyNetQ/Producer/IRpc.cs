using System;
using System.Threading.Tasks;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    /// An RPC style request-response pattern
    /// </summary>
    public interface IRpc
    {
        /// <summary>
        /// Make a request to an RPC service
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="request">The request message</param>
        /// <returns>Returns a task that yields the result when the response arrives</returns>
        Task<TResponse> Request<TRequest, TResponse>(TRequest request)
            where TRequest : class
            where TResponse : class;

        /// <summary>
        /// Set up a responder for an RPC service.
        /// </summary>
        /// <typeparam name="TRequest">The request type</typeparam>
        /// <typeparam name="TResponse">The response type</typeparam>
        /// <param name="responder">A function that performs the response</param>
        void Respond<TRequest, TResponse>(Func<TRequest, Task<TResponse>> responder)
            where TRequest : class
            where TResponse : class;
    }

    /// <summary>
    /// Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class Rpc : IRpc
    {
        private readonly IAdvancedBus advancedBus;
        private readonly IEventBus eventBus;
        private readonly IConventions conventions;

        public Rpc(IAdvancedBus advancedBus, IEventBus eventBus, IConventions conventions)
        {
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");

            this.advancedBus = advancedBus;
            this.eventBus = eventBus;
            this.conventions = conventions;
        }

        public Task<TResponse> Request<TRequest, TResponse>(TRequest request) 
            where TRequest : class 
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var taskCompletionSource = new TaskCompletionSource<TResponse>();

            var returnQueueName = SubscribeToResponse<TResponse>(response => taskCompletionSource.TrySetResult(response));
            RequestPublish(request, returnQueueName);

            return taskCompletionSource.Task;
        }

        private string SubscribeToResponse<TResponse>(Action<TResponse> onResponse)
            where TResponse : class
        {
            var queue = advancedBus.QueueDeclare(
                conventions.RpcReturnQueueNamingConvention(),
                passive: false,
                durable: false,
                exclusive: true,
                autoDelete: true).SetAsSingleUse();

            advancedBus.Consume<TResponse>(queue, (message, messageRecievedInfo) =>
            {
                var tcs = new TaskCompletionSource<object>();

                try
                {
                    onResponse(message.Body);
                    tcs.SetResult(null);
                }
                catch (Exception exception)
                {
                    tcs.SetException(exception);
                }
                return tcs.Task;
            });

            return queue.Name;
        }

        private void RequestPublish<TRequest>(TRequest request, string returnQueueName) where TRequest : class
        {
            var routingKey = conventions.RpcRoutingKeyNamingConvention(typeof(TRequest));
            var exchange = advancedBus.ExchangeDeclare(conventions.RpcExchangeNamingConvention(), ExchangeType.Direct);

            var requestMessage = new Message<TRequest>(request);
            requestMessage.Properties.ReplyTo = returnQueueName;

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