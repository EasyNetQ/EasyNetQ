using EasyNetQ.Configuration;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ;

/// <summary>
///     Publishes through the transport: on first use it connects, declares the routes' exchanges and builds one
///     publish pipeline per definition; every publish then rents a pooled context and runs its route's pipeline.
/// </summary>
public sealed class TransportMessagePublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IEnumerable<PublishDefinition> definitions;
    private readonly ITransport transport;
    private readonly IServiceProvider services;
    private readonly PipelineBuilder<PublishContext> publishPipelineBuilder;
    private readonly IMessageSerializer messageSerializer;
    private readonly IMessageTypeRegistry registry;
    private readonly BusOptions busOptions;

    private readonly SemaphoreSlim initLock = new(1, 1);
    private volatile Runtime? runtime;
    private ITransportConnection? connection;
    private ITransportChannel? channel;

    private sealed class Runtime
    {
        public required IReadOnlyDictionary<Type, PublishRoute> Routes { get; init; }
        public required ContextPool<PublishContext> ContextPool { get; init; }
    }

    /// <summary>
    ///     Creates the publisher
    /// </summary>
    public TransportMessagePublisher(
        IEnumerable<PublishDefinition> definitions,
        ITransport transport,
        IServiceProvider services,
        PipelineBuilder<PublishContext> publishPipelineBuilder,
        IMessageSerializer messageSerializer,
        IMessageTypeRegistry registry
    )
    {
        this.definitions = definitions;
        this.transport = transport;
        this.services = services;
        this.publishPipelineBuilder = publishPipelineBuilder;
        this.messageSerializer = messageSerializer;
        this.registry = registry;
        busOptions = services.GetService<BusOptions>() ?? new BusOptions();
    }

    /// <inheritdoc />
    public ValueTask PublishAsync<T>(T message, CancellationToken cancellationToken = default)
        => PublishInternalAsync(message, null, cancellationToken);

    /// <inheritdoc />
    public ValueTask PublishAsync<T>(T message, string routingKey, CancellationToken cancellationToken = default)
    {
        if (routingKey is null)
            throw new ArgumentNullException(nameof(routingKey));
        return PublishInternalAsync(message, routingKey, cancellationToken);
    }

    private async ValueTask PublishInternalAsync<T>(T message, string? routingKeyOverride, CancellationToken cancellationToken)
    {
        var currentRuntime = runtime ?? await InitializeAsync(cancellationToken).ConfigureAwait(false);
        if (!currentRuntime.Routes.TryGetValue(typeof(T), out var route))
            throw new InvalidOperationException(
                $"No publish route is registered for {typeof(T)}. Register one with Publish(p => p.Exchange(...).Message<{typeof(T).Name}>(...))"
            );

        var context = currentRuntime.ContextPool.Rent();
        try
        {
            context.Exchange = route.Definition.Exchange;
            context.RoutingKey = routingKeyOverride
                ?? (route is PublishRoute<T> typedRoute ? typedRoute.ResolveRoutingKey(message) : route.ResolveRoutingKey(message!));
            context.Mandatory = route.Definition.Mandatory ?? false;
            context.PublisherConfirms = route.Definition.PublisherConfirms ?? busOptions.PublisherConfirms;
            context.MessageType = route.Descriptor;
            context.Message = message;

            using var cts = cancellationToken.WithTimeout(busOptions.Timeout);
            context.CancellationToken = cts.Token;

            await route.Pipeline!(context).ConfigureAwait(false);
        }
        finally
        {
            currentRuntime.ContextPool.Return(context);
        }
    }

    private async ValueTask<Runtime> InitializeAsync(CancellationToken cancellationToken)
    {
        await initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (runtime is { } initialized)
                return initialized;

            var connectionContext = new ConnectionContext("Publishers", services);
            connectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Producer);
            connection = await transport.ConnectAsync(connectionContext, cancellationToken).ConfigureAwait(false);
            var channelContext = new ChannelContext(connectionContext);
            channel = await connection.OpenChannelAsync(channelContext, cancellationToken).ConfigureAwait(false);
            var topology = channel.Topology;

            var correlationIdGenerator = services.GetRequiredService<ICorrelationIdGenerationStrategy>();
            var table = new PublishRouteTable(registry);
            var pipelines = new Dictionary<PublishDefinition, PipelineStep<PublishContext>>();
            var publishChannel = channel;
            foreach (var definition in definitions)
            {
                if (topology is not null && definition.ExchangeToDeclare is { } exchange)
                    await topology.DeclareExchangeAsync(exchange, cancellationToken).ConfigureAwait(false);

                foreach (var registration in definition.MessageRegistrations)
                    registration(table);

                var pipelineBuilder = publishPipelineBuilder.Clone()
                    .UseSerialize(new SerializeStep(messageSerializer, correlationIdGenerator, busOptions.PersistentMessages));
                definition.MessagePipeline?.Invoke(pipelineBuilder);

                pipelines[definition] = pipelineBuilder.Build(services, context => publishChannel.PublishAsync(context));
            }

            foreach (var route in table.Routes.Values)
                route.Pipeline = pipelines[route.Definition];

            var built = new Runtime
            {
                Routes = table.Routes,
                ContextPool = new ContextPool<PublishContext>(() => new PublishContext(channelContext)),
            };
            runtime = built;
            return built;
        }
        finally
        {
            initLock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (channel is not null)
            await channel.DisposeAsync().ConfigureAwait(false);
        if (connection is not null)
            await connection.DisposeAsync().ConfigureAwait(false);
        initLock.Dispose();
    }
}
