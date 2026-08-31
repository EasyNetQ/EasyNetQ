using EasyNetQ.Configuration;
using EasyNetQ.Diagnostics;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace EasyNetQ.Hosting;

/// <summary>
///     Starts the fluent-registered consumers: declares their topology, builds their message pipelines and runs
///     them on the transport for the lifetime of the host
/// </summary>
public sealed class ConsumerHostedService : IHostedService
{
    private readonly IEnumerable<ConsumerDefinition> definitions;
    private readonly ITransport transport;
    private readonly IServiceProvider services;
    private readonly PipelineBuilder<ConsumeContext> consumePipelineBuilder;
    private readonly IMessageSerializer messageSerializer;
    private readonly IMessageTypeRegistry registry;

    private readonly List<ITransportConsumer> consumers = new();
    private ITransportChannel? channel;

    /// <summary>
    ///     Creates the service
    /// </summary>
    public ConsumerHostedService(
        IEnumerable<ConsumerDefinition> definitions,
        ITransport transport,
        IServiceProvider services,
        PipelineBuilder<ConsumeContext> consumePipelineBuilder,
        IMessageSerializer messageSerializer,
        IMessageTypeRegistry registry
    )
    {
        this.definitions = definitions;
        this.transport = transport;
        this.services = services;
        this.consumePipelineBuilder = consumePipelineBuilder;
        this.messageSerializer = messageSerializer;
        this.registry = registry;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var connectionContext = new ConnectionContext("Consumers", services);
        connectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Consumer);
        var connection = await transport.ConnectAsync(connectionContext, cancellationToken).ConfigureAwait(false);
        var channelContext = new ChannelContext(connectionContext);
        channel = await connection.OpenChannelAsync(channelContext, cancellationToken).ConfigureAwait(false);
        var topology = channel.Topology;
        var busOptions = services.GetService<BusOptions>() ?? new BusOptions();
        var telemetryOptions = services.GetService<TelemetryOptions>();

        foreach (var definition in definitions)
        {
            var queueName = definition.Queue;
            if (topology is not null)
            {
                foreach (var exchange in definition.ExchangesToDeclare)
                    await topology.DeclareExchangeAsync(exchange, cancellationToken).ConfigureAwait(false);
                if (definition.QueueToDeclare is { } queueDefinition)
                    queueName = await topology.DeclareQueueAsync(queueDefinition, cancellationToken).ConfigureAwait(false);
                foreach (var binding in definition.Bindings)
                    await topology.BindAsync(
                        new BindingDefinition(binding.Exchange, queueName, binding.RoutingKey) { Arguments = binding.Arguments },
                        cancellationToken
                    ).ConfigureAwait(false);
            }

            var handlers = new HandlerTable(registry);
            foreach (var registration in definition.HandlerRegistrations)
                registration(services, handlers);

            var consumerContext = new ConsumerContext(channelContext, queueName)
            {
                PrefetchCount = definition.PrefetchCount ?? busOptions.PrefetchCount,
                AutoAck = definition.AutoAck,
                Handlers = handlers,
            };
            if (telemetryOptions is not null)
                consumerContext.Set(Keys.ConsumerTelemetry, new ConsumerTelemetry(queueName, telemetryOptions.MessagingSystem));
            definition.ConfigureContext?.Invoke(consumerContext);

            var pipelineBuilder = consumePipelineBuilder.Clone().UseTypedDispatch(messageSerializer);
            definition.MessagePipeline?.Invoke(pipelineBuilder);
            consumerContext.MessagePipeline = pipelineBuilder.Build(services, DispatchTerminal);

            consumers.Add(await channel.StartConsumerAsync([consumerContext], cancellationToken).ConfigureAwait(false));
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
            await consumer.DisposeAsync().ConfigureAwait(false);
        consumers.Clear();
        if (channel is not null)
            await channel.DisposeAsync().ConfigureAwait(false);
    }

    private static async ValueTask DispatchTerminal(ConsumeContext context)
        => context.Ack = await context.Handler!.InvokeAsync(context).ConfigureAwait(false);
}
