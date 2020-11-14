using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ
{
    /// <summary>
    ///     Default implementation of EasyNetQ's request-response pattern
    /// </summary>
    public class DefaultRpc : IRpc
    {
        protected const string IsFaultedKey = "IsFaulted";
        protected const string ExceptionMessageKey = "ExceptionMessage";
        protected readonly IAdvancedBus advancedBus;
        private readonly ConnectionConfiguration configuration;
        protected readonly IConventions conventions;
        private readonly ICorrelationIdGenerationStrategy correlationIdGenerationStrategy;
        private readonly IDisposable eventSubscription;
        protected readonly IExchangeDeclareStrategy exchangeDeclareStrategy;

        protected readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

        private readonly ConcurrentDictionary<string, ResponseAction> responseActions =
            new ConcurrentDictionary<string, ResponseAction>();

        private readonly ConcurrentDictionary<RpcKey, ResponseSubscription> responseSubscriptions =
            new ConcurrentDictionary<RpcKey, ResponseSubscription>();

        private readonly AsyncLock responseSubscriptionsLock = new AsyncLock();
        private readonly ITypeNameSerializer typeNameSerializer;

        public DefaultRpc(
            ConnectionConfiguration configuration,
            IAdvancedBus advancedBus,
            IEventBus eventBus,
            IConventions conventions,
            IExchangeDeclareStrategy exchangeDeclareStrategy,
            IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
            ITypeNameSerializer typeNameSerializer,
            ICorrelationIdGenerationStrategy correlationIdGenerationStrategy
        )
        {
            Preconditions.CheckNotNull(configuration, "configuration");
            Preconditions.CheckNotNull(advancedBus, "advancedBus");
            Preconditions.CheckNotNull(eventBus, "eventBus");
            Preconditions.CheckNotNull(conventions, "conventions");
            Preconditions.CheckNotNull(exchangeDeclareStrategy, "publishExchangeDeclareStrategy");
            Preconditions.CheckNotNull(messageDeliveryModeStrategy, "messageDeliveryModeStrategy");
            Preconditions.CheckNotNull(typeNameSerializer, "typeNameSerializer");
            Preconditions.CheckNotNull(correlationIdGenerationStrategy, "correlationIdGenerationStrategy");

            this.configuration = configuration;
            this.advancedBus = advancedBus;
            this.conventions = conventions;
            this.exchangeDeclareStrategy = exchangeDeclareStrategy;
            this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
            this.typeNameSerializer = typeNameSerializer;
            this.correlationIdGenerationStrategy = correlationIdGenerationStrategy;

            eventSubscription = eventBus.Subscribe<ConnectionRecoveredEvent>(OnConnectionRecovered);
        }

        /// <inheritdoc />
        public virtual async Task<TResponse> RequestAsync<TRequest, TResponse>(
            TRequest request,
            Action<IRequestConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(request, "request");

            var requestType = typeof(TRequest);
            var requestConfiguration = new RequestConfiguration(
                conventions.RpcRoutingKeyNamingConvention(requestType),
                configuration.Timeout
            );
            configure(requestConfiguration);

            using var cts = cancellationToken.WithTimeout(requestConfiguration.Expiration);

            var correlationId = correlationIdGenerationStrategy.GetCorrelationId();
            var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
            RegisterResponseActions(correlationId, tcs);
            using var callback = DisposableAction.Create(DeRegisterResponseActions, correlationId);

            var queueName = await SubscribeToResponseAsync<TRequest, TResponse>(cts.Token).ConfigureAwait(false);
            var routingKey = requestConfiguration.QueueName;
            var expiration = requestConfiguration.Expiration;
            var priority = requestConfiguration.Priority;
            await RequestPublishAsync(
                request, routingKey, queueName, correlationId, expiration, priority, configuration.MandatoryPublish, cts.Token
            ).ConfigureAwait(false);
            tcs.AttachCancellation(cts.Token);
            return await tcs.Task.ConfigureAwait(false);
        }

        /// <inheritdoc />
        public virtual AwaitableDisposable<IDisposable> RespondAsync<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder,
            Action<IResponderConfiguration> configure,
            CancellationToken cancellationToken = default
        )
        {
            Preconditions.CheckNotNull(responder, "responder");
            Preconditions.CheckNotNull(configure, "configure");
            // We're explicitly validating TResponse here because the type won't be used directly.
            // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
            Preconditions.CheckShortString(typeNameSerializer.Serialize(typeof(TResponse)), "TResponse");

            return RespondAsyncInternal(responder, configure, cancellationToken).ToAwaitableDisposable();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            eventSubscription.Dispose();
            foreach (var responseSubscription in responseSubscriptions.Values)
                responseSubscription.Unsubscribe();
        }

        private void OnConnectionRecovered(ConnectionRecoveredEvent @event)
        {
            var responseActionsValues = responseActions.Values;
            var responseSubscriptionsValues = responseSubscriptions.Values;

            responseActions.Clear();
            responseSubscriptions.Clear();

            foreach (var responseAction in responseActionsValues) responseAction.OnFailure();
            foreach (var responseSubscription in responseSubscriptionsValues) responseSubscription.Unsubscribe();
        }

        protected void DeRegisterResponseActions(string correlationId)
        {
            responseActions.Remove(correlationId);
        }

        protected void RegisterResponseActions<TResponse>(string correlationId, TaskCompletionSource<TResponse> tcs)
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
                            exceptionMessage =
                                Encoding.UTF8.GetString((byte[]) msg.Properties.Headers[ExceptionMessageKey]);
                    }

                    if (isFaulted)
                        tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                    else
                        tcs.TrySetResult(msg.Body);
                },
                () => tcs.TrySetException(
                    new EasyNetQException("Connection lost while request was in-flight. CorrelationId: {0}",
                        correlationId))
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
                    conventions.RpcReturnQueueNamingConvention(responseType),
                    c => c.AsDurable(false).AsExclusive(true).AsAutoDelete(true),
                    cancellationToken
                ).ConfigureAwait(false);

                var exchangeName = conventions.RpcResponseExchangeNamingConvention(responseType);
                if (exchangeName != Exchange.GetDefault().Name)
                {
                    var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                        exchangeName,
                        ExchangeType.Direct,
                        cancellationToken
                    ).ConfigureAwait(false);
                    await advancedBus.BindAsync(exchange, queue, queue.Name, cancellationToken).ConfigureAwait(false);
                }

                var subscription = advancedBus.Consume<TResponse>(queue, (message, messageReceivedInfo) =>
                {
                    if (responseActions.TryRemove(message.Properties.CorrelationId, out var responseAction))
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
            string correlationId,
            TimeSpan expiration,
            byte? priority,
            bool mandatory,
            CancellationToken cancellationToken
        )
        {
            var requestType = typeof(TRequest);
            var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                conventions.RpcRequestExchangeNamingConvention(requestType),
                ExchangeType.Direct,
                cancellationToken
            ).ConfigureAwait(false);

            var properties = new MessageProperties
            {
                ReplyTo = returnQueueName,
                CorrelationId = correlationId,
                DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType)
            };
            if (expiration != Timeout.InfiniteTimeSpan)
                properties.Expiration = expiration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            if (priority != null)
                properties.Priority = priority.Value;

            var requestMessage = new Message<TRequest>(request, properties);
            await advancedBus.PublishAsync(exchange, routingKey, mandatory, requestMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<IDisposable> RespondAsyncInternal<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder,
            Action<IResponderConfiguration> configure,
            CancellationToken cancellationToken
        )
        {
            var requestType = typeof(TRequest);

            var responderConfiguration = new ResponderConfiguration(configuration.PrefetchCount);
            configure(responderConfiguration);

            var routingKey = responderConfiguration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);

            var exchange = await advancedBus.ExchangeDeclareAsync(
                conventions.RpcRequestExchangeNamingConvention(requestType),
                ExchangeType.Direct,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            var queue = await advancedBus.QueueDeclareAsync(
                routingKey,
                c =>
                {
                    c.AsDurable(responderConfiguration.Durable);
                    if (responderConfiguration.Expires != null)
                        c.WithExpires(responderConfiguration.Expires.Value);
                    if (responderConfiguration.MaxPriority.HasValue)
                        c.WithMaxPriority(responderConfiguration.MaxPriority.Value);
                },
                cancellationToken
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, routingKey, cancellationToken).ConfigureAwait(false);

            return advancedBus.Consume<TRequest>(
                queue,
                (m, i, c) => RespondToMessageAsync(responder, m, c),
                c => c.WithPrefetchCount(responderConfiguration.PrefetchCount)
            );
        }

        private async Task RespondToMessageAsync<TRequest, TResponse>(
            Func<TRequest, CancellationToken, Task<TResponse>> responder,
            IMessage<TRequest> requestMessage,
            CancellationToken cancellationToken
        )
        {
            //TODO Cache declaration of exchange
            var exchangeName = conventions.RpcResponseExchangeNamingConvention(typeof(TResponse));
            var exchange = exchangeName == Exchange.GetDefault().Name
                ? Exchange.GetDefault()
                : await advancedBus.ExchangeDeclareAsync(
                    exchangeName,
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
