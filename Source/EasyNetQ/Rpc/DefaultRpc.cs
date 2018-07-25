using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;
using Newtonsoft.Json;

namespace EasyNetQ.Rpc
{
    /// <summary>
    /// Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class DefaultRpc : IRpc
    {
        private readonly ConnectionConfiguration connectionConfiguration;
        protected readonly IAdvancedBus advancedBus;
        protected readonly IConventions conventions;
        protected readonly IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy;
        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        private readonly ITimeoutStrategy timeoutStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        private readonly AsyncLock responseQueuesAddLock = new AsyncLock();
        private readonly ConcurrentDictionary<RpcKey, string> responseQueues = new ConcurrentDictionary<RpcKey, string>();
        private readonly ConcurrentDictionary<Guid, ResponseAction> responseActions = new ConcurrentDictionary<Guid, ResponseAction>();

        protected readonly TimeSpan disablePeriodicSignaling = TimeSpan.FromMilliseconds(-1);

        protected const string isFaultedKey = "IsFaulted";
        protected const string exceptionMessageKey = "ExceptionMessage";

        public DefaultRpc(
            ConnectionConfiguration connectionConfiguration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IPublishExchangeDeclareStrategy publishExchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITimeoutStrategy timeoutStrategy,
            ITypeNameSerializer typeNameSerializer)
        {
            Preconditions.CheckNotNull(connectionConfiguration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(publishExchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(timeoutStrategy, "timeoutStrategy");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.connectionConfiguration = connectionConfiguration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.publishExchangeDeclareStrategy = publishExchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.timeoutStrategy = timeoutStrategy;
            this.typeNameSerializer = typeNameSerializer;

            eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated);
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var copyOfResponseActions = responseActions.Values;
            responseActions.Clear();
            responseQueues.Clear();

            // retry in-flight requests.
            foreach (var responseAction in copyOfResponseActions)
            {
                responseAction.OnFailure();
            }
        }

        public virtual async Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken
        )
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();
            var requestType = typeof(TRequest);
            var configuration = new RequestConfiguration();
            configure(configuration);

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            { 
                var timeoutSeconds = timeoutStrategy.GetTimeoutSeconds(requestType);
                if (timeoutSeconds > 0)
                {
                    cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
                }

                using (cts.Token.Register(() => UnregisterResponseActions(correlationId)))
                {
                    //TODO Complete tcs without hijacking
                    var tcs = new TaskCompletionSource<TResponse>();

                    RegisterResponseActions(correlationId, tcs);
                    var queueName = await SubscribeToResponseAsync<TRequest, TResponse>(cancellationToken).ConfigureAwait(false);
                    var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);
                    await RequestPublishAsync(request, routingKey, queueName, correlationId, cancellationToken).ConfigureAwait(false);
                    
                    return await tcs.Task.ConfigureAwait(false);
                }                
            }
        }

        protected void UnregisterResponseActions(Guid correlationId)
        {
            responseActions.TryRemove(correlationId, out _);
        }

        protected void RegisterResponseActions<TResponse>(Guid correlationId, TaskCompletionSource<TResponse> tcs)
            where TResponse : class
        {
            responseActions.TryAdd(correlationId, new ResponseAction
            {
                OnSuccess = message =>
                {
                    var msg = (IMessage<TResponse>)message;

                    var isFaulted = false;
                    var exceptionMessage = "The exception message has not been specified.";
                    if(msg.Properties.HeadersPresent)
                    {
                        if(msg.Properties.Headers.ContainsKey(isFaultedKey))
                        {
                            isFaulted = Convert.ToBoolean(msg.Properties.Headers[isFaultedKey]);
                        }
                        if(msg.Properties.Headers.ContainsKey(exceptionMessageKey))
                        {
                            exceptionMessage = Encoding.UTF8.GetString((byte[])msg.Properties.Headers[exceptionMessageKey]);
                        }
                    }

                    if(isFaulted)
                    {
                        tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                    }
                    else
                    {
                        tcs.TrySetResult(msg.Body);
                    }
                },
                OnFailure = () =>
                {
                    tcs.TrySetException(new EasyNetQException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId.ToString()));
                }
            });
        }

        protected virtual async Task<string> SubscribeToResponseAsync<TRequest, TResponse>(CancellationToken cancellationToken)
            where TResponse : class
        {
            var responseType = typeof(TResponse);
            var rpcKey = new RpcKey { Request = typeof(TRequest), Response = responseType };
            if (responseQueues.TryGetValue(rpcKey, out var queueName))
                return queueName;
            
            using(await responseQueuesAddLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (responseQueues.TryGetValue(rpcKey, out queueName))
                    return queueName;

                var queue = await advancedBus.QueueDeclareAsync(
                            conventions.RpcReturnQueueNamingConvention(),
                            false,
                            false,
                            true,
                            true,
                            cancellationToken: cancellationToken
                    ).ConfigureAwait(false);

                var exchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(
                    conventions.RpcResponseExchangeNamingConvention(responseType),
                    ExchangeType.Direct,
                    cancellationToken
                ).ConfigureAwait(false);

                await advancedBus.BindAsync(exchange, queue, queue.Name, cancellationToken).ConfigureAwait(false);

                //TODO store IDisposable consumer in responseQueues and to dispose it later
                advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) =>
                {
                    if(Guid.TryParse(message.Properties.CorrelationId, out var correlationId) && responseActions.TryRemove(correlationId, out var responseAction))
                    {
                        responseAction.OnSuccess(message);
                    }
                });
                
                responseQueues.TryAdd(rpcKey, queue.Name);
                return queue.Name;
            }
        }

        protected struct RpcKey
        {
            public Type Request;
            public Type Response;
        }

        protected class ResponseAction
        {
            public Action<object> OnSuccess { get; set; }
            public Action OnFailure { get; set; }
        }

        protected virtual async Task RequestPublishAsync<TRequest>(
            TRequest request,
            string routingKey,
            string returnQueueName, 
            Guid correlationId,
            CancellationToken cancellationToken
        ) where TRequest : class
        {
            var requestType = typeof(TRequest);
            var exchange = await publishExchangeDeclareStrategy.DeclareExchangeAsync(
                conventions.RpcRequestExchangeNamingConvention(requestType),
                ExchangeType.Direct,
                cancellationToken
            ).ConfigureAwait(false);
                        
            var requestMessage = new Message<TRequest>(request)
            {
                Properties =
                {
                    ReplyTo = returnQueueName,
                    CorrelationId = correlationId.ToString(),
                    Expiration = (timeoutStrategy.GetTimeoutSeconds(requestType) * 1000).ToString(),
                    DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType)
                }
            };

            await advancedBus.PublishAsync(exchange, routingKey, false, requestMessage, cancellationToken).ConfigureAwait(false);
        }

        public virtual AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder, 
            Action<IResponderConfiguration> configure,
            CancellationToken cancellationToken
        ) 
            where TRequest : class
            where TResponse : class
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitly validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(TResponse)), "TResponse");

            return RespondAsyncInternal(responder, configure).ToAwaitableDisposable();
        }

        private async Task<IDisposable> RespondAsyncInternal<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> responder, Action<IResponderConfiguration> configure) where TRequest : class where TResponse : class
        {
            var requestType = typeof(TRequest);

            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);

            var exchange = await advancedBus.ExchangeDeclareAsync(conventions.RpcRequestExchangeNamingConvention(requestType), ExchangeType.Direct).ConfigureAwait(false);
            var queue = await advancedBus.QueueDeclareAsync(routingKey).ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, routingKey).ConfigureAwait(false);

            return advancedBus.Consume<TRequest>(
                queue,
                (requestMessage, messageReceivedInfo) => ExecuteResponder(responder, requestMessage),
                c => c.WithPrefetchCount(configuration.PrefetchCount)
            );
        }

        protected Task ExecuteResponder<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> responder, IMessage<TRequest> requestMessage)
            where TRequest : class
            where TResponse : class
        {
            var tcs = new TaskCompletionSource<object>();

            try
            {
                responder(requestMessage.Body, default).ContinueWith(task =>
                {
                    if (task.IsFaulted || task.IsCanceled)
                    {
                        var exception = task.IsCanceled 
                            ? new EasyNetQResponderException("The responder task was cancelled.")
                            : task.Exception?.InnerException ?? new EasyNetQResponderException("The responder faulted while dispatching the message.");


                        OnResponderFailure<TRequest, TResponse>(requestMessage, exception.Message, exception);
                        tcs.SetException(exception);
                    }
                    else
                    {
                        OnResponderSuccess(requestMessage, task.Result);
                        tcs.SetResult(null);
                    }
                });
            }
            catch (Exception e)
            {
                OnResponderFailure<TRequest, TResponse>(requestMessage, e.Message, e);
                tcs.SetException(e);
            }

            return tcs.Task;
        }

        protected virtual void OnResponderSuccess<TRequest, TResponse>(IMessage<TRequest> requestMessage, TResponse response)
            where TRequest : class
            where TResponse : class
        {
            var responseMessage = new Message<TResponse>(response)
            {
                Properties =
                {
                    CorrelationId = requestMessage.Properties.CorrelationId,
                    DeliveryMode = MessageDeliveryMode.NonPersistent
                }
            };

            var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(exchange, requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        protected virtual void OnResponderFailure<TRequest, TResponse>(IMessage<TRequest> requestMessage, string exceptionMessage, Exception exception)
            where TRequest : class
            where TResponse : class
        {
            // HACK: I think we can live with this, because it will run only on exception, 
            // it tries to preserve the default serialization behavior, 
            // being able to also deserialize POCO objects that has constructors with parameters
            // this avoids to introduce a custom class wrapper that will change the message payload
            var body = JsonConvert.DeserializeObject<TResponse>(typeof(TResponse) == typeof(string) ? "''" : "{}");

            var responseMessage = new Message<TResponse>(body);
            responseMessage.Properties.Headers.Add(isFaultedKey, true);
            responseMessage.Properties.Headers.Add(exceptionMessageKey, exceptionMessage);
            responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
            responseMessage.Properties.DeliveryMode = MessageDeliveryMode.NonPersistent;

            var exchange = DeclareRpcExchange(conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)));

            advancedBus.Publish(exchange, requestMessage.Properties.ReplyTo, false, responseMessage);
        }

        private IExchange DeclareRpcExchange(string exchangeName)
        {
            return publishExchangeDeclareStrategy.DeclareExchange(exchangeName, ExchangeType.Direct);
        }
    }
}