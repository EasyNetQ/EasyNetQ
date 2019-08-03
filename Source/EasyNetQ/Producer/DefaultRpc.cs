using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.FluentConfiguration;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.Producer
{
    /// <summary>
    ///     Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class DefaultRpc : IRpc
    {
        protected const string IsFaultedKey = "IsFaulted";
        protected const string ExceptionMessageKey = "ExceptionMessage";
        protected readonly IAdvancedBus advancedBus;
        private readonly ConnectionConfiguration connectionConfiguration;
        protected readonly IConventions conventions;
        private readonly List<IDisposable> eventBusSubscriptions = new List<IDisposable>();

        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
        protected readonly IExchangeDeclareStrategy exchangeDeclareStrategy;
        private readonly ConcurrentDictionary<Guid, ResponseAction> responseActions = new ConcurrentDictionary<Guid, ResponseAction>();
        private readonly ConcurrentDictionary<RpcKey, ResponseSubscription> responseSubscriptions = new ConcurrentDictionary<RpcKey, ResponseSubscription>();

        private readonly AsyncLock responseSubscriptionsLock = new AsyncLock();
        private readonly ITimeoutStrategy timeoutStrategy;
        private readonly ITypeNameSerializer typeNameSerializer;

        public DefaultRpc(
            ConnectionConfiguration connectionConfiguration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITimeoutStrategy timeoutStrategy,
            ITypeNameSerializer typeNameSerializer
        )
        {
            Preconditions.CheckNotNull(connectionConfiguration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(exchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(timeoutStrategy, "timeoutStrategy");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");

            this.connectionConfiguration = connectionConfiguration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.timeoutStrategy = timeoutStrategy;
            this.typeNameSerializer = typeNameSerializer;

            eventBusSubscriptions.Add(eventBus.Subscribe<ConnectionCreatedEvent>(OnConnectionCreated));
        }

        public virtual async Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(request, "request");

            var correlationId = Guid.NewGuid();
            var requestType = typeof(TRequest);
            var configuration = new RequestConfiguration();
            configure(configuration);

            using (var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timeoutSeconds = timeoutStrategy.GetTimeoutSeconds(requestType);
                if (timeoutSeconds > 0) cts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var tcs = TaskHelpers.CreateTcs<TResponse>();
                RegisterResponseActions(correlationId, tcs);

                using (cts.Token.Register(() => DeRegisterResponseActions(correlationId)))
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    try
                    {
                        var queueName = await SubscribeToResponseAsync<TRequest, TResponse>(cts.Token).ConfigureAwait(false);
                        var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);
                        await RequestPublishAsync(request, routingKey, queueName, correlationId, cts.Token).ConfigureAwait(false);

                        return await tcs.Task.ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        throw new TimeoutException();
                    }
                }
            }
        }

        public virtual AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder,
            Action<IResponderConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitly validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(TResponse)), "TResponse");

            return RespondAsyncInternal(responder, configure, cancellationToken).ToAwaitableDisposable();
        }

        public void Dispose()
        {
            foreach (var eventBusSubscription in eventBusSubscriptions)
                eventBusSubscription.Dispose();

            foreach (var responseSubscription in responseSubscriptions.Values)
                responseSubscription.Unsubscribe();
        }

        private void OnConnectionCreated(ConnectionCreatedEvent @event)
        {
            var responseActionsValues = responseActions.Values;
            var responseSubscriptionsValues = responseSubscriptions.Values;

            responseActions.Clear();
            responseSubscriptions.Clear();

            foreach (var responseAction in responseActionsValues) responseAction.OnFailure();
            foreach (var responseSubscription in responseSubscriptionsValues) responseSubscription.Unsubscribe();
        }

        protected void DeRegisterResponseActions(Guid correlationId)
        {
            responseActions.TryRemove(correlationId, out _);
        }

        protected void RegisterResponseActions<TResponse>(Guid correlationId, TaskCompletionSource<TResponse> tcs)
        {
            var responseAction = new ResponseAction(
                message =>
                {
                    var msg = (IMessage<TResponse>) message;

                    var isFaulted = false;
                    var exceptionMessage = "The exception message has not been specified.";
                    if (msg.Properties.HeadersPresent)
                    {
                        if (msg.Properties.Headers.ContainsKey(IsFaultedKey))
                            isFaulted = Convert.ToBoolean(msg.Properties.Headers[IsFaultedKey]);
                        if (msg.Properties.Headers.ContainsKey(ExceptionMessageKey))
                            exceptionMessage = Encoding.UTF8.GetString((byte[]) msg.Properties.Headers[ExceptionMessageKey]);
                    }

                    if (isFaulted)
                        tcs.TrySetExceptionAsynchronously(new EasyNetQResponderException(exceptionMessage));
                    else
                        tcs.TrySetResultAsynchronously(msg.Body);
                },
                () => tcs.TrySetExceptionAsynchronously(new EasyNetQException("Connection lost while request was in-flight. CorrelationId: {0}", correlationId))
            );

            responseActions.TryAdd(correlationId, responseAction);
        }

        protected virtual async Task<string> SubscribeToResponseAsync<TRequest, TResponse>(CancellationToken cancellationToken)
        {
            var responseType = typeof(TResponse);
            var requestType = typeof(TRequest);
            var rpcKey = new RpcKey(requestType, responseType);
            if (responseSubscriptions.TryGetValue(rpcKey, out var responseSubscription))
                return responseSubscription.QueueName;

            using (await responseSubscriptionsLock.AcquireAsync(cancellationToken).ConfigureAwait(false))
            {
                if (responseSubscriptions.TryGetValue(rpcKey, out responseSubscription))
                    return responseSubscription.QueueName;

                var queue = await advancedBus.QueueDeclareAsync(
                    conventions.RpcReturnQueueNamingConvention(),
                    c => c.AsDurable(false).AsExclusive(true).AsAutoDelete(true),
                    cancellationToken
                ).ConfigureAwait(false);

                var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                    conventions.RpcResponseExchangeNamingConvention(responseType),
                    ExchangeType.Direct,
                    cancellationToken
                ).ConfigureAwait(false);

                await advancedBus.BindAsync(exchange, queue, queue.Name, cancellationToken).ConfigureAwait(false);

                var subscription = advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) =>
                {
                    if (Guid.TryParse(message.Properties.CorrelationId, out var correlationId) && responseActions.TryRemove(correlationId, out var responseAction))
                        responseAction.OnSuccess(message);
                });

                responseSubscriptions.TryAdd(rpcKey, new ResponseSubscription(queue.Name, subscription));
                return queue.Name;
            }
        }

        protected virtual async Task RequestPublishAsync<TRequest>(
            TRequest request,
            string routingKey,
            string returnQueueName,
            Guid correlationId,
            CancellationToken cancellationToken
        )
        {
            var requestType = typeof(TRequest);
            var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
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

        private async Task<IDisposable> RespondAsyncInternal<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> responder,
            Action<IResponderConfiguration> configure, CancellationToken cancellationToken)
        {
            var requestType = typeof(TRequest);

            var configuration = new ResponderConfiguration(connectionConfiguration.PrefetchCount);
            configure(configuration);

            var routingKey = configuration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);

            var exchange = await advancedBus.ExchangeDeclareAsync(
                conventions.RpcRequestExchangeNamingConvention(requestType),
                ExchangeType.Direct,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var queue = await advancedBus.QueueDeclareAsync(routingKey, cancellationToken).ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, routingKey, cancellationToken).ConfigureAwait(false);

            return advancedBus.Consume<TRequest>(
                queue,
                (m, i, c) => RespondToMessageAsync(responder, m, c),
                c => c.WithPrefetchCount(configuration.PrefetchCount)
            );
        }

        private async Task RespondToMessageAsync<TRequest, TResponse>(Func<TRequest, CancellationToken, Task<TResponse>> responder, IMessage<TRequest> requestMessage,
            CancellationToken cancellationToken)
        {
            //TODO Cache declaration of exchange
            var exchange = await advancedBus.ExchangeDeclareAsync(
                conventions.RpcResponseExchangeNamingConvention(typeof(TResponse)),
                ExchangeType.Direct,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

            try
            {
                var request = requestMessage.Body;
                var response = await responder(request, cancellationToken).ConfigureAwait(false);
                var responseMessage = new Message<TResponse>(response)
                {
                    Properties =
                    {
                        CorrelationId = requestMessage.Properties.CorrelationId,
                        DeliveryMode = MessageDeliveryMode.NonPersistent
                    }
                };
                await advancedBus.PublishAsync(
                    exchange,
                    requestMessage.Properties.ReplyTo,
                    false,
                    responseMessage,
                    cancellationToken
                ).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                var responseMessage = new Message<TResponse>();
                responseMessage.Properties.Headers.Add(IsFaultedKey, true);
                responseMessage.Properties.Headers.Add(ExceptionMessageKey, exception.Message);
                responseMessage.Properties.CorrelationId = requestMessage.Properties.CorrelationId;
                responseMessage.Properties.DeliveryMode = MessageDeliveryMode.NonPersistent;

                await advancedBus.PublishAsync(
                    exchange,
                    requestMessage.Properties.ReplyTo,
                    false,
                    responseMessage,
                    cancellationToken
                ).ConfigureAwait(false);

                throw;
            }
        }

        protected struct RpcKey
        {
            public RpcKey(Type requestType, Type responseType)
            {
                RequestType = requestType;
                ResponseType = responseType;
            }

            public Type RequestType { get; }
            public Type ResponseType { get; }
        }

        protected struct ResponseAction
        {
            public ResponseAction(Action<object> onSuccess, Action onFailure)
            {
                OnSuccess = onSuccess;
                OnFailure = onFailure;
            }

            public Action<object> OnSuccess { get; }
            public Action OnFailure { get; }
        }

        protected struct ResponseSubscription
        {
            public ResponseSubscription(string queueName, IDisposable subscription)
            {
                QueueName = queueName;
                Unsubscribe = subscription.Dispose;
            }

            public string QueueName { get; }
            public Action Unsubscribe { get; }
        }
    }
}
