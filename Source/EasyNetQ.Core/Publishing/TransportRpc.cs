using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using EasyNetQ.Diagnostics;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
///     Request-response over <see cref="ITransport" />: the request publishes through a pipeline with
///     <see cref="SerializeStep" />, the per-response-type reply consumer resolves the correlation id back to the
///     waiting request. Works on any transport; the RabbitMQ package registers its own <see cref="IRpc" /> until
///     the facades finish moving here.
/// </summary>
public sealed class TransportRpc : IRpc, IAsyncDisposable
{
    private const string IsFaultedKey = "IsFaulted";
    private const string ExceptionMessageKey = "ExceptionMessage";

    private readonly ITransport transport;
    private readonly IServiceProvider services;
    private readonly IMessageTypeRegistry registry;
    private readonly IMessageSerializer messageSerializer;
    private readonly PipelineBuilder<PublishContext> publishPipelineBuilder;
    private readonly PipelineBuilder<ConsumeContext> consumePipelineBuilder;
    private readonly IConventions conventions;
    private readonly ICorrelationIdGenerationStrategy correlationIdGenerationStrategy;
    private readonly IMessageDeliveryModeStrategy messageDeliveryModeStrategy;
    private readonly BusOptions busOptions;
    private readonly string messagingSystem;

    private readonly SemaphoreSlim initLock = new(1, 1);
    private volatile Runtime? runtime;
    private readonly ConcurrentDictionary<string, Task> declaredExchanges = new();
    private readonly ConcurrentDictionary<string, ResponseAction> responseActions = new();
    private readonly ConcurrentDictionary<RpcKey, ResponseSubscription> responseSubscriptions = new();
    private readonly AsyncLock responseSubscriptionsLock = new();
    private readonly List<ITransportConsumer> responderConsumers = new();

    private sealed class Runtime
    {
        public required ITransportConnection ProducerConnection { get; init; }
        public required ITransportConnection ConsumerConnection { get; init; }
        public required ITransportChannel ProducerChannel { get; init; }
        public required ITransportChannel ConsumerChannel { get; init; }
        public required ChannelContext ConsumerChannelContext { get; init; }
        public required PipelineStep<PublishContext> PublishPipeline { get; init; }
        public required ContextPool<PublishContext> PublishContextPool { get; init; }
    }

    /// <summary>
    ///     Creates the rpc
    /// </summary>
    public TransportRpc(
        ITransport transport,
        IServiceProvider services,
        IMessageTypeRegistry registry,
        IMessageSerializer messageSerializer,
        PipelineBuilder<PublishContext> publishPipelineBuilder,
        PipelineBuilder<ConsumeContext> consumePipelineBuilder,
        IConventions conventions,
        ICorrelationIdGenerationStrategy correlationIdGenerationStrategy,
        IMessageDeliveryModeStrategy messageDeliveryModeStrategy
    )
    {
        this.transport = transport;
        this.services = services;
        this.registry = registry;
        this.messageSerializer = messageSerializer;
        this.publishPipelineBuilder = publishPipelineBuilder;
        this.consumePipelineBuilder = consumePipelineBuilder;
        this.conventions = conventions;
        this.correlationIdGenerationStrategy = correlationIdGenerationStrategy;
        this.messageDeliveryModeStrategy = messageDeliveryModeStrategy;
        busOptions = services.GetService<BusOptions>() ?? new BusOptions();
        messagingSystem = services.GetService<TelemetryOptions>()?.MessagingSystem ?? "rabbitmq";
    }

    /// <inheritdoc />
    public async Task<TResponse> RequestAsync<TRequest, TResponse>(
        TRequest request,
        Action<IRequestConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        ValidateWireName<TResponse>();

        var requestType = typeof(TRequest);
        var requestConfiguration = new RequestConfiguration(
            conventions.RpcRoutingKeyNamingConvention(requestType),
            busOptions.Timeout,
            conventions.QueueTypeConvention(requestType)
        );
        configure(requestConfiguration);

        using var cts = cancellationToken.WithTimeout(requestConfiguration.Expiration);

        var correlationId = correlationIdGenerationStrategy.GetCorrelationId();

        using var rpcActivity = EasyNetQDiagnostics.Source.HasListeners()
            ? EasyNetQDiagnostics.Source.StartActivity($"rpc {requestConfiguration.QueueName}", ActivityKind.Client)
            : null;
        if (rpcActivity is not null)
        {
            rpcActivity.SetTag(MessagingTags.MessagingSystem, messagingSystem);
            rpcActivity.SetTag(MessagingTags.OperationName, "rpc");
            rpcActivity.SetTag(MessagingTags.DestinationName, requestConfiguration.QueueName);
            rpcActivity.SetTag(MessagingTags.ConversationId, correlationId);
        }

        var tcs = new TaskCompletionSource<TResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        RegisterResponseAction(correlationId, tcs, rpcActivity?.Context ?? default);
        using var deRegistration = DisposableAction.Create(
            static state => state.self.responseActions.TryRemove(state.correlationId, out _), (self: this, correlationId)
        );

        var currentRuntime = runtime ?? await InitializeAsync(cts.Token).ConfigureAwait(false);
        var replyQueueName = await SubscribeToResponseAsync<TRequest, TResponse>(
            currentRuntime, requestConfiguration.QueueType, cts.Token
        ).ConfigureAwait(false);

        await PublishRequestAsync(
            currentRuntime, request, requestConfiguration, replyQueueName, correlationId, cts.Token
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

    /// <inheritdoc />
    public Task<IAsyncDisposable> RespondAsync<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        Action<IResponderConfiguration> configure,
        CancellationToken cancellationToken = default
    )
    {
        ValidateWireName<TResponse>();
        return RespondInternalAsync(responder, configure, cancellationToken);
    }

    private async Task<IAsyncDisposable> RespondInternalAsync<TRequest, TResponse>(
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        Action<IResponderConfiguration> configure,
        CancellationToken cancellationToken
    )
    {
        var requestType = typeof(TRequest);
        var responderConfiguration = new ResponderConfiguration(busOptions.PrefetchCount, conventions.QueueTypeConvention(requestType));
        configure(responderConfiguration);

        var routingKey = responderConfiguration.QueueName ?? conventions.RpcRoutingKeyNamingConvention(requestType);
        var currentRuntime = runtime ?? await InitializeAsync(cancellationToken).ConfigureAwait(false);
        var topology = currentRuntime.ConsumerChannel.Topology;

        var exchangeName = conventions.RpcRequestExchangeNamingConvention(requestType);
        if (topology is not null)
        {
            await DeclareExchangeOnceAsync(topology, exchangeName, cancellationToken).ConfigureAwait(false);
            var queueName = await topology.DeclareQueueAsync(
                new QueueDefinition(routingKey, responderConfiguration.Durable) { Arguments = responderConfiguration.QueueArguments },
                cancellationToken
            ).ConfigureAwait(false);
            if (exchangeName.Length > 0)
                await topology.BindAsync(new BindingDefinition(exchangeName, queueName, routingKey), cancellationToken).ConfigureAwait(false);
        }

        var handlers = new HandlerTable(registry);
        handlers.Add<TRequest>(async (requestBody, context) =>
        {
            await RespondToMessageAsync(currentRuntime, responder, requestBody, context.Properties, context.CancellationToken).ConfigureAwait(false);
            return AckDecision.Ack;
        });

        var consumer = await StartTypedConsumerAsync(
            currentRuntime, routingKey, handlers, responderConfiguration.PrefetchCount, cancellationToken
        ).ConfigureAwait(false);
        lock (responderConsumers)
        {
            responderConsumers.Add(consumer);
        }
        return new ResponderDisposal(this, consumer);
    }

    private sealed class ResponderDisposal : IAsyncDisposable
    {
        private readonly TransportRpc rpc;
        private readonly ITransportConsumer consumer;

        public ResponderDisposal(TransportRpc rpc, ITransportConsumer consumer)
        {
            this.rpc = rpc;
            this.consumer = consumer;
        }

        public ValueTask DisposeAsync()
        {
            lock (rpc.responderConsumers)
            {
                rpc.responderConsumers.Remove(consumer);
            }
            return consumer.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        foreach (var responseSubscription in responseSubscriptions.Values)
            await responseSubscription.Consumer.DisposeAsync().ConfigureAwait(false);
        ITransportConsumer[] consumers;
        lock (responderConsumers)
        {
            consumers = responderConsumers.ToArray();
            responderConsumers.Clear();
        }
        foreach (var consumer in consumers)
            await consumer.DisposeAsync().ConfigureAwait(false);
        if (runtime is { } currentRuntime)
        {
            await currentRuntime.ProducerChannel.DisposeAsync().ConfigureAwait(false);
            await currentRuntime.ConsumerChannel.DisposeAsync().ConfigureAwait(false);
            await currentRuntime.ProducerConnection.DisposeAsync().ConfigureAwait(false);
            await currentRuntime.ConsumerConnection.DisposeAsync().ConfigureAwait(false);
        }
        initLock.Dispose();
        responseSubscriptionsLock.Dispose();
    }

    private void ValidateWireName<TResponse>()
    {
        var wireName = registry.GetOrAdd<TResponse>().WireName;
        if (wireName.Length > 255)
            throw new ArgumentOutOfRangeException(nameof(TResponse), typeof(TResponse), "Must be less than or equal to 255 characters when serialized.");
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

    private void RegisterResponseAction<TResponse>(
        string correlationId, TaskCompletionSource<TResponse> tcs, in ActivityContext requestActivityContext
    )
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
                    if (properties.Headers.TryGetValue(ExceptionMessageKey, out var exceptionMessageValue))
                        exceptionMessage = Encoding.UTF8.GetString((byte[])exceptionMessageValue!);
                }

                if (isFaulted)
                    tcs.TrySetException(new EasyNetQResponderException(exceptionMessage));
                else
                    tcs.TrySetResult((TResponse)body!);
            },
            requestActivityContext
        );

        responseActions.TryAdd(correlationId, responseAction);
    }

    private async Task<string> SubscribeToResponseAsync<TRequest, TResponse>(
        Runtime currentRuntime, string queueType, CancellationToken cancellationToken
    )
    {
        var rpcKey = new RpcKey(typeof(TRequest), typeof(TResponse));
        if (responseSubscriptions.TryGetValue(rpcKey, out var responseSubscription))
            return responseSubscription.QueueName;

        using var _ = await responseSubscriptionsLock.AcquireAsync(cancellationToken).ConfigureAwait(false);

        if (responseSubscriptions.TryGetValue(rpcKey, out responseSubscription))
            return responseSubscription.QueueName;

        var queueIsQuorum = queueType == QueueType.Quorum;
        var topology = currentRuntime.ConsumerChannel.Topology;
        var queueName = conventions.RpcReturnQueueNamingConvention(typeof(TResponse));
        if (topology is not null)
        {
            queueName = await topology.DeclareQueueAsync(
                new QueueDefinition(queueName, Durable: queueIsQuorum, Exclusive: !queueIsQuorum, AutoDelete: !queueIsQuorum),
                cancellationToken
            ).ConfigureAwait(false);

            var responseExchangeName = conventions.RpcResponseExchangeNamingConvention(typeof(TResponse));
            if (responseExchangeName.Length > 0)
            {
                await DeclareExchangeOnceAsync(topology, responseExchangeName, cancellationToken).ConfigureAwait(false);
                await topology.BindAsync(new BindingDefinition(responseExchangeName, queueName, queueName), cancellationToken).ConfigureAwait(false);
            }
        }

        var handlers = new HandlerTable(registry);
        handlers.Add<TResponse>((body, context) =>
        {
            var properties = context.Properties;
            if (properties.CorrelationId is not null && responseActions.TryRemove(properties.CorrelationId, out var responseAction))
            {
#if NET9_0_OR_GREATER
                if (responseAction.RequestActivityContext != default)
                    Activity.Current?.AddLink(new ActivityLink(responseAction.RequestActivityContext));
#endif
                responseAction.OnSuccess(properties, body);
            }
            return new ValueTask<AckDecision>(AckDecision.Ack);
        });

        var consumer = await StartTypedConsumerAsync(
            currentRuntime, queueName, handlers, busOptions.PrefetchCount, cancellationToken
        ).ConfigureAwait(false);
        responseSubscriptions.TryAdd(rpcKey, new ResponseSubscription(queueName, consumer));
        return queueName;
    }

    private async Task<ITransportConsumer> StartTypedConsumerAsync(
        Runtime currentRuntime, string queueName, HandlerTable handlers, ushort prefetchCount, CancellationToken cancellationToken
    )
    {
        var consumerContext = new ConsumerContext(currentRuntime.ConsumerChannelContext, queueName)
        {
            PrefetchCount = prefetchCount,
            Handlers = handlers,
        };
        if (services.GetService<TelemetryOptions>() is { } telemetryOptions)
            consumerContext.Set(Keys.ConsumerTelemetry, new ConsumerTelemetry(queueName, telemetryOptions.MessagingSystem));
        consumerContext.MessagePipeline = consumePipelineBuilder.Clone()
            .UseTypedDispatch(messageSerializer)
            .Build(services, DispatchTerminal);
        return await currentRuntime.ConsumerChannel.StartConsumerAsync([consumerContext], cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishRequestAsync<TRequest>(
        Runtime currentRuntime,
        TRequest request,
        RequestConfiguration requestConfiguration,
        string replyQueueName,
        string correlationId,
        CancellationToken cancellationToken
    )
    {
        var requestType = typeof(TRequest);
        var exchangeName = conventions.RpcRequestExchangeNamingConvention(requestType);
        if (currentRuntime.ProducerChannel.Topology is { } topology)
            await DeclareExchangeOnceAsync(topology, exchangeName, cancellationToken).ConfigureAwait(false);

        var context = currentRuntime.PublishContextPool.Rent();
        try
        {
            context.Exchange = exchangeName;
            context.RoutingKey = requestConfiguration.QueueName;
            context.PublisherConfirms = requestConfiguration.PublisherConfirms ?? busOptions.PublisherConfirms;
            context.Properties = new MessageProperties
            {
                ReplyTo = replyQueueName,
                CorrelationId = correlationId,
                Priority = requestConfiguration.Priority ?? 0,
                Headers = requestConfiguration.MessageHeaders,
                DeliveryMode = messageDeliveryModeStrategy.GetDeliveryMode(requestType),
                Expiration = requestConfiguration.Expiration == Timeout.InfiniteTimeSpan ? null : requestConfiguration.Expiration,
            };
            context.MessageType = registry.GetOrAdd<TRequest>();
            context.Message = request;
            context.CancellationToken = cancellationToken;

            await currentRuntime.PublishPipeline(context).ConfigureAwait(false);
        }
        finally
        {
            currentRuntime.PublishContextPool.Return(context);
        }
    }

    private async Task RespondToMessageAsync<TRequest, TResponse>(
        Runtime currentRuntime,
        Func<TRequest, CancellationToken, Task<TResponse>> responder,
        TRequest request,
        MessageProperties requestProperties,
        CancellationToken cancellationToken
    )
    {
        var responseExchangeName = conventions.RpcResponseExchangeNamingConvention(typeof(TResponse));
        if (responseExchangeName.Length > 0 && currentRuntime.ProducerChannel.Topology is { } topology)
            await DeclareExchangeOnceAsync(topology, responseExchangeName, cancellationToken).ConfigureAwait(false);

        TResponse? response = default;
        Exception? failure = null;
        try
        {
            response = await responder(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            failure = exception;
        }

        var properties = new MessageProperties
        {
            CorrelationId = requestProperties.CorrelationId,
            DeliveryMode = MessageDeliveryMode.NonPersistent,
            Headers = failure is null
                ? null
                : new Dictionary<string, object>
                {
                    { IsFaultedKey, true },
                    { ExceptionMessageKey, Encoding.UTF8.GetBytes(failure.Message) },
                },
        };

        var context = currentRuntime.PublishContextPool.Rent();
        try
        {
            context.Exchange = responseExchangeName;
            context.RoutingKey = requestProperties.ReplyTo!;
            context.Properties = properties;
            context.MessageType = registry.GetOrAdd<TResponse>();
            context.Message = failure is null ? response : null;
            context.CancellationToken = cancellationToken;

            await currentRuntime.PublishPipeline(context).ConfigureAwait(false);
        }
        finally
        {
            currentRuntime.PublishContextPool.Return(context);
        }
    }

    private Task DeclareExchangeOnceAsync(ITopology topology, string exchangeName, CancellationToken cancellationToken)
    {
        if (exchangeName.Length == 0)
            return Task.CompletedTask;

        var declare = declaredExchanges.GetOrAdd(
            exchangeName,
            name => topology.DeclareExchangeAsync(new ExchangeDefinition(name, ExchangeType.Direct), cancellationToken).AsTask()
        );
        if (declare.IsFaulted || declare.IsCanceled)
            declaredExchanges.TryRemove(exchangeName, out _);
        return declare;
    }

    private async ValueTask<Runtime> InitializeAsync(CancellationToken cancellationToken)
    {
        await initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (runtime is { } initialized)
                return initialized;

            var producerConnectionContext = new ConnectionContext("RpcProducer", services);
            producerConnectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Producer);
            var producerConnection = await transport.ConnectAsync(producerConnectionContext, cancellationToken).ConfigureAwait(false);
            var producerChannelContext = new ChannelContext(producerConnectionContext);
            var producerChannel = await producerConnection.OpenChannelAsync(producerChannelContext, cancellationToken).ConfigureAwait(false);

            var consumerConnectionContext = new ConnectionContext("RpcConsumer", services);
            consumerConnectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Consumer);
            var consumerConnection = await transport.ConnectAsync(consumerConnectionContext, cancellationToken).ConfigureAwait(false);
            var consumerChannelContext = new ChannelContext(consumerConnectionContext);
            var consumerChannel = await consumerConnection.OpenChannelAsync(consumerChannelContext, cancellationToken).ConfigureAwait(false);

            var publishPipeline = publishPipelineBuilder.Clone()
                .UseSerialize(new SerializeStep(messageSerializer, correlationIdGenerationStrategy, busOptions.PersistentMessages))
                .Build(services, context => producerChannel.PublishAsync(context));

            var built = new Runtime
            {
                ProducerConnection = producerConnection,
                ConsumerConnection = consumerConnection,
                ProducerChannel = producerChannel,
                ConsumerChannel = consumerChannel,
                ConsumerChannelContext = consumerChannelContext,
                PublishPipeline = publishPipeline,
                PublishContextPool = new ContextPool<PublishContext>(() => new PublishContext(producerChannelContext)),
            };
            runtime = built;
            return built;
        }
        finally
        {
            initLock.Release();
        }
    }

    private static async ValueTask DispatchTerminal(ConsumeContext context)
        => context.Ack = await context.Handler!.InvokeAsync(context).ConfigureAwait(false);

    private readonly record struct RpcKey(Type RequestType, Type ResponseType);

    private readonly struct ResponseAction
    {
        public ResponseAction(Action<MessageProperties, object?> onSuccess, in ActivityContext requestActivityContext)
        {
            OnSuccess = onSuccess;
            RequestActivityContext = requestActivityContext;
        }

        public Action<MessageProperties, object?> OnSuccess { get; }
        public ActivityContext RequestActivityContext { get; }
    }

    private readonly struct ResponseSubscription
    {
        public ResponseSubscription(string queueName, ITransportConsumer consumer)
        {
            QueueName = queueName;
            Consumer = consumer;
        }

        public string QueueName { get; }
        public ITransportConsumer Consumer { get; }
    }
}
