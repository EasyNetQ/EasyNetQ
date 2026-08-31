using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using EasyNetQ.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Topology;
using Microsoft.Extensions.Logging;

namespace EasyNetQ;

/// <summary>
///     Default implementation of EasyNetQ's request-response pattern
/// </summary>

public sealed class DefaultRpc : IRpc, IAsyncDisposable
{
    const string IsFaultedKey = "IsFaulted";
    const string ExceptionMessageKey = "ExceptionMessage";
    readonly IAdvancedBus advancedBus;
    private readonly ILogger<DefaultRpc> logger;
    private readonly BusOptions configuration;
    readonly IConventions conventions;
    private readonly ICorrelationIdGenerationStrategy correlationIdGenerationStrategy;
    private readonly IDisposable eventSubscription;
    readonly IExchangeDeclareStrategy exchangeDeclareStrategy;

    readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;

    private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new();

    private readonly ConcurrentDictionary<RpcKey, ResponseSubscription> responseSubscriptions = new();

    private readonly AsyncLock responseSubscriptionsLock = new();
    private readonly ITypeNameSerializer typeNameSerializer;

    public DefaultRpc(
        ILogger<DefaultRpc> logger,
        BusOptions configuration,
        IAdvancedBus advancedBus,
        IEventBus eventBus,
        IConventions conventions,
        IExchangeDeclareStrategy exchangeDeclareStrategy,
        IMessageDeliveryModeStrategy messageDeliveryModeStrategy,
        ITypeNameSerializer typeNameSerializer,
        ICorrelationIdGenerationStrategy correlationIdGenerationStrategy
    )
    {
        this.logger = logger;
        this.configuration = configuration;
        this.advancedBus = advancedBus;
        this.conventions = conventions;
        this.exchangeDeclareStrategy = exchangeDeclareStrategy;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        this.typeNameSerializer = typeNameSerializer;
        this.correlationIdGenerationStrategy = correlationIdGenerationStrategy;

        eventSubscription = eventBus.Subscribe<ConnectionRestoredEvent>(OnConnectionRestored);
    }

    /// <inheritdoc />
    public async Task<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        Action<IRequestConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        var requestType = typeof(TRequest);
        var requestConfiguration = new RequestConfiguration(
            conventions.RpcRoutingKeyNamingConvention(requestType),
            configuration.Timeout,
            conventions.QueueTypeConvention(requestType)
        );
        configure(requestConfiguration);

        using var cts = cancellationToken.WithTimeout(requestConfiguration.Expiration);

        var correlationId = correlationIdGenerationStrategy.GetCorrelationId();

        // one CLIENT span for the whole request/response round trip; the response's process span links back
        using var rpcActivity = EasyNetQDiagnostics.Source.HasListeners()
            ? EasyNetQDiagnostics.Source.StartActivity($"rpc {requestConfiguration.QueueName}", ActivityKind.Client)
            : null;
        if (rpcActivity is not null)
        {
            rpcActivity.SetTag(MessagingTags.MessagingSystem, "rabbitmq");
            rpcActivity.SetTag(MessagingTags.OperationName, "rpc");
            rpcActivity.SetTag(MessagingTags.DestinationName, requestConfiguration.QueueName);
            rpcActivity.SetTag(MessagingTags.ConversationId, correlationId);
        }

        var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterResponseActions(correlationId, tcs, requestConfiguration.QueueType == QueueType.Quorum, rpcActivity?.Context ?? default);
        using var callback = DisposableAction.Create(DeRegisterResponseActions, correlationId);

        var queueName = await SubscribeToResponseAsync<TRequest, TResponse>(requestConfiguration.QueueType, cts.Token).ConfigureAwait(false);
        var routingKey = requestConfiguration.QueueName;
        var expiration = requestConfiguration.Expiration;
        var priority = requestConfiguration.Priority;
        var headers = requestConfiguration.MessageHeaders;
        await RequestPublishAsync(
            request,
            routingKey,
            queueName,
            correlationId,
            expiration,
            priority,
            null,
            requestConfiguration.PublisherConfirms,
            headers,
            cts.Token
        ).ConfigureAwait(false);
        tcs.AttachCancellation(cts.Token);
        try
        {
            return await tcs.Task.ConfigureAwait(false);
        }
        catch (Exception exception) when (MarkRpcFailure(rpcActivity, exception))
        {
            throw; // MarkRpcFailure never handles, it only tags the span
        }
    }

    private static bool MarkRpcFailure(Activity? activity, Exception exception)
    {
        if (activity is not null)
        {
            activity.SetTag(MessagingTags.ErrorType, exception.GetType().FullName);
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
        return false;
    }

    /// <inheritdoc />
    public Task<IAsyncDisposable> RespondAsync<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        Action<IResponderConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        // We're explicitly validating TResponse here because the type won't be used directly.
        // It'll only be used when executing a successful responder, which will silently fail if TResponse serialized length exceeds the limit.
        var serializedResponse = typeNameSerializer.Serialize(typeof(TResponse));
        if (serializedResponse.Length > 255)
            throw new ArgumentOutOfRangeException(nameof(TResponse), typeof(TResponse), "Must be less than or equal to 255 characters when serialized.");

        return RespondAsyncInternal(responder, configure, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        eventSubscription.Dispose();
        foreach (var responseSubscription in responseSubscriptions.Values)
            await responseSubscription.Unsubscribe();
    }

    private Task OnConnectionRestored(ConnectionRestoredEvent messageEvent)
    {
        if (messageEvent.Type != PersistentConnectionType.Consumer)
            return Task.CompletedTask;

        List<KeyValuePair<RpcKey, ResponseSubscription>> subEntries = responseSubscriptions.ToList();
        List<KeyValuePair<string, ResponseAction>> actionEntries = responseActions.ToList();

        foreach (KeyValuePair<RpcKey, ResponseSubscription> subEntry in subEntries)
        {
            if (!subEntry.Value.QueueIsDurable)
            {
                responseSubscriptions.TryRemove(subEntry.Key, out _);
                subEntry.Value.Unsubscribe();
            }
        }

        foreach (KeyValuePair<string, ResponseAction> actionEntry in actionEntries)
        {
            if (!actionEntry.Value.QueueIsDurable)
            {
                DeRegisterResponseActions(actionEntry.Key);
                actionEntry.Value.OnFailure();
            }
        }
        return Task.CompletedTask;
    }

    void DeRegisterResponseActions(string correlationId)
    {
        responseActions.TryRemove(correlationId, out _);
    }

    void RegisterResponseActions<TResponse>(string correlationId, TaskCompletionSource<TResponse> tcs, bool queueIsDurable, in ActivityContext requestActivityContext)
    {
        var responseAction = new ResponseAction(
            (properties, body) =>
            {
                var isFaulted = false;
                var exceptionMessage = "The exception message has not been specified.";

                if (properties is { HeadersPresent: true, Headers: not null })
                {
                    if (properties.Headers.TryGetValue(IsFaultedKey, out var isFaultedValue))
                        isFaulted = Convert.ToBoolean(isFaultedValue);
                    if (properties.Headers.TryGetValue(ExceptionMessageKey, out var exchangeMessageValue))
                        exceptionMessage = Encoding.UTF8.GetString((byte[])exchangeMessageValue!);
                }

                if (isFaulted)
                    tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                else
                    tcs.TrySetResult((TResponse)body!);
            },
            () => tcs.TrySetException(
                new EasyNetQException(
                    $"Connection lost while request was in-flight. CorrelationId: {correlationId}"
                )
            ), queueIsDurable, requestActivityContext
        );

        responseActions.TryAdd(correlationId, responseAction);
    }

    async Task<string> SubscribeToResponseAsync<TRequest, TResponse>(string queueType,
        CancellationToken cancellationToken
    )
    {
        var responseType = typeof(TResponse);
        var requestType = typeof(TRequest);
        var rpcKey = new RpcKey(requestType, responseType);
        if (responseSubscriptions.TryGetValue(rpcKey, out var responseSubscription))
            return responseSubscription.QueueName;

        logger.RpcSubscribing(requestType, responseType);

        using var _ = await responseSubscriptionsLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        if (responseSubscriptions.TryGetValue(rpcKey, out responseSubscription))
            return responseSubscription.QueueName;
        bool queueIsQuorum = queueType == QueueType.Quorum;
        var queue = await advancedBus.QueueDeclareAsync(
            conventions.RpcReturnQueueNamingConvention(responseType),
            durable: queueIsQuorum,
            exclusive: !queueIsQuorum,
            autoDelete: !queueIsQuorum,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var exchangeName = conventions.RpcResponseExchangeNamingConvention(responseType);
        if (exchangeName != Exchange.Default.Name)
        {
            var exchange = await exchangeDeclareStrategy.DeclareExchangeAsync(
                exchangeName, ExchangeType.Direct, cancellationToken
            ).ConfigureAwait(false);
            await advancedBus.BindAsync(exchange, queue, queue.Name, cancellationToken).ConfigureAwait(false);
        }

        var subscription = await advancedBus.ConsumeAsync<TResponse>(
            queue,
            (body, context) =>
            {
                var properties = context.Properties;
                if (properties.CorrelationId != null && responseActions.TryRemove(properties.CorrelationId, out var responseAction))
                {
#if NET9_0_OR_GREATER
                    if (responseAction.RequestActivityContext != default)
                        Activity.Current?.AddLink(new ActivityLink(responseAction.RequestActivityContext));
#endif
                    responseAction.OnSuccess(properties, body);
                }
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }
        );
        responseSubscriptions.TryAdd(rpcKey, new ResponseSubscription(queue.Name, subscription, queueIsQuorum));

        logger.RpcSubscriptionCreated(requestType, responseType);

        return queue.Name;
    }

    async Task RequestPublishAsync<TRequest>(
        TRequest request,
        string routingKey,
        string returnQueueName,
        string correlationId,
        TimeSpan expiration,
        byte? priority,
        bool? mandatory,
        bool? publisherConfirms,
        IDictionary<string, object> headers,
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
            Priority = priority ?? 0,
            Headers = headers,
            DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType),
            Expiration = expiration == Timeout.InfiniteTimeSpan ? null : expiration
        };

        await advancedBus.PublishAsync(exchange.Name, routingKey, mandatory, publisherConfirms, properties, request, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<IAsyncDisposable> RespondAsyncInternal<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        Action<IResponderConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        var requestType = typeof(TRequest);

        var responderConfiguration = new ResponderConfiguration(configuration.PrefetchCount, conventions.QueueTypeConvention(typeof(TRequest)));
        configure(responderConfiguration);

        var routingKey = responderConfiguration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);

        var exchange = await advancedBus.ExchangeDeclareAsync(
            exchange: conventions.RpcRequestExchangeNamingConvention(requestType),
            type: ExchangeType.Direct,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var queue = await advancedBus.QueueDeclareAsync(
            queue: routingKey,
            durable: responderConfiguration.Durable,
            arguments: responderConfiguration.QueueArguments,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        await advancedBus.BindAsync(exchange, queue, routingKey, cancellationToken).ConfigureAwait(false);

        return await advancedBus.ConsumeAsync<TRequest>(
            queue,
            (message, _, cancellation) => RespondToMessageAsync(responder, message, cancellation),
            c => c.WithPrefetchCount(responderConfiguration.PrefetchCount)
        );
    }

    private async Task RespondToMessageAsync<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        IMessage<TRequest> requestMessage,
        CancellationToken cancellationToken
    )
    {
        var responseExchangeName = conventions.RpcResponseExchangeNamingConvention(typeof(TResponse));
        var responseExchange = responseExchangeName == Exchange.Default.Name
            ? Exchange.Default
            : await exchangeDeclareStrategy.DeclareExchangeAsync(
                responseExchangeName,
                ExchangeType.Direct,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);

        try
        {
            var request = requestMessage.Body!;
            var response = await responder(request, cancellationToken).ConfigureAwait(false);
            var responseProperties = new MessageProperties
            {
                CorrelationId = requestMessage.Properties.CorrelationId,
                DeliveryMode = MessageDeliveryMode.NonPersistent
            };
            await advancedBus.PublishAsync(
                responseExchange.Name,
                requestMessage.Properties.ReplyTo!,
                false,
                null,
                responseProperties,
                response,
                cancellationToken
            ).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var faultProperties = new MessageProperties
            {
                CorrelationId = requestMessage.Properties.CorrelationId,
                DeliveryMode = MessageDeliveryMode.NonPersistent,
                Headers = new Dictionary<string, object>
                {
                    { IsFaultedKey, true },
                    { ExceptionMessageKey, Encoding.UTF8.GetBytes(exception.Message) }
                }
            };
            await advancedBus.PublishAsync<TResponse>(
                responseExchange.Name,
                requestMessage.Properties.ReplyTo!,
                false,
                null,
                faultProperties,
                default!,
                cancellationToken
            ).ConfigureAwait(false);

            throw;
        }
    }

    readonly record struct RpcKey(Type RequestType, Type ResponseType);

    readonly struct ResponseAction
    {
        public ResponseAction(Action<MessageProperties, object?> onSuccess, Action onFailure, bool queueIsDurable, in ActivityContext requestActivityContext)
        {
            OnSuccess = onSuccess;
            OnFailure = onFailure;
            QueueIsDurable = queueIsDurable;
            RequestActivityContext = requestActivityContext;
        }

        public bool QueueIsDurable { get; }
        public Action<MessageProperties, object?> OnSuccess { get; }
        public Action OnFailure { get; }
        public ActivityContext RequestActivityContext { get; }
    }

    readonly struct ResponseSubscription
    {
        public ResponseSubscription(string queueName, IAsyncDisposable subscription, bool queueIsDurable)
        {
            QueueIsDurable = queueIsDurable;
            QueueName = queueName;
            Unsubscribe = subscription.DisposeAsync;
        }
        public bool QueueIsDurable { get; }
        public string QueueName { get; }
        public Func<ValueTask> Unsubscribe { get; }
    }
}
