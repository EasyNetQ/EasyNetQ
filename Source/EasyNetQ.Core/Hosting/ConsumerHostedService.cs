using EasyNetQ.Configuration;
using EasyNetQ.Diagnostics;
using EasyNetQ.Internals;
using EasyNetQ.Persistent;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using EasyNetQ.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyNetQ.Hosting;

/// <summary>
///     Starts the fluent-registered consumers: declares their topology, builds their message pipelines and runs
///     them on the transport for the lifetime of the host. Startup runs in the background and retries each consumer
///     until it runs (see <see cref="ConsumerHostOptions" />); <see cref="IConsumerHostStatus" /> reports progress.
/// </summary>
public sealed class ConsumerHostedService : IHostedService
{
    private readonly IEnumerable<ConsumerDefinition> definitions;
    private readonly ITransport transport;
    private readonly IServiceProvider services;
    private readonly PipelineBuilder<ConsumeContext> consumePipelineBuilder;
    private readonly IMessageSerializer messageSerializer;
    private readonly IMessageTypeRegistry registry;
    private readonly ConsumerHostOptions options;
    private readonly ConsumerHostStatus status;
    private readonly ILogger<ConsumerHostedService> logger;

    private readonly List<ITransportConsumer> consumers = new();
    private readonly CancellationTokenSource stopping = new();
    private ChannelContext? channelContext;
    private ITransportChannel? channel;
    private Task? startup;

    /// <summary>
    ///     Creates the service
    /// </summary>
    public ConsumerHostedService(
        IEnumerable<ConsumerDefinition> definitions,
        ITransport transport,
        IServiceProvider services,
        PipelineBuilder<ConsumeContext> consumePipelineBuilder,
        IMessageSerializer messageSerializer,
        IMessageTypeRegistry registry,
        ConsumerHostOptions options,
        ConsumerHostStatus status,
        ILogger<ConsumerHostedService> logger
    )
    {
        this.definitions = definitions;
        this.transport = transport;
        this.services = services;
        this.consumePipelineBuilder = consumePipelineBuilder;
        this.messageSerializer = messageSerializer;
        this.registry = registry;
        this.options = options;
        this.status = status;
        this.logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var pending = definitions.ToList();
        status.Pending(pending.Count);
        if (options.WaitForStartup)
            return StartConsumersAsync(pending, cancellationToken);

        startup = Task.Run(() => StartConsumersAsync(pending, stopping.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        stopping.Cancel();
        if (startup is not null)
        {
            try
            {
                await startup.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        foreach (var consumer in consumers)
            await consumer.DisposeAsync().ConfigureAwait(false);
        consumers.Clear();
        if (channel is not null)
            await channel.DisposeAsync().ConfigureAwait(false);
        channel = null;
    }

    private async Task StartConsumersAsync(List<ConsumerDefinition> pending, CancellationToken cancellationToken)
    {
        var delay = options.RetryDelay;
        while (true)
        {
            if (channel is null)
            {
                try
                {
                    channel = await OpenChannelAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    status.Failed(exception);
                    logger.ConsumerHostConnectFailed(exception, delay);
                }
            }

            if (channel is not null)
            {
                for (var i = 0; i < pending.Count;)
                {
                    try
                    {
                        consumers.Add(await StartConsumerAsync(channel, pending[i], cancellationToken).ConfigureAwait(false));
                        pending.RemoveAt(i);
                        status.Pending(pending.Count);
                    }
                    catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                    {
                        status.Failed(exception);
                        logger.ConsumerStartFailed(exception, pending[i].Queue, delay);
                        await NotifyStartFailedAsync(exception, cancellationToken).ConfigureAwait(false);
                        i++;
                    }
                }
            }

            if (pending.Count == 0)
            {
                status.Started();
                logger.ConsumersStarted(consumers.Count);
                return;
            }

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            delay = TimeSpan.FromTicks(Math.Min(delay.Ticks * 2, options.MaxRetryDelay.Ticks));
        }
    }

    private async Task<ITransportChannel> OpenChannelAsync(CancellationToken cancellationToken)
    {
        var connectionContext = new ConnectionContext("Consumers", services);
        connectionContext.Set(Keys.ConnectionType, PersistentConnectionType.Consumer);
        var connection = await transport.ConnectAsync(connectionContext, cancellationToken).ConfigureAwait(false);
        channelContext = new ChannelContext(connectionContext);
        return await connection.OpenChannelAsync(channelContext, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ITransportConsumer> StartConsumerAsync(ITransportChannel transportChannel, ConsumerDefinition definition, CancellationToken cancellationToken)
    {
        var topology = transportChannel.Topology;
        var busOptions = services.GetService<BusOptions>() ?? new BusOptions();
        var telemetryOptions = services.GetService<TelemetryOptions>();

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

        var consumerContext = new ConsumerContext(channelContext!, queueName)
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

        return await transportChannel.StartConsumerAsync([consumerContext], cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask NotifyStartFailedAsync(Exception exception, CancellationToken cancellationToken)
    {
        if (channelContext is null || services.GetService<LifecycleNotifier>() is not { IsEnabled: true } notifier) return;
        try
        {
            await notifier.NotifyAsync(channelContext, LifecycleLayer.Consumer, LifecycleEvent.StartFailed, exception.Message, exception, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception notifyException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.FailedToHandleEvent(notifyException, nameof(LifecycleEvent.StartFailed));
        }
    }

    private static async ValueTask DispatchTerminal(ConsumeContext context)
        => context.Ack = await context.Handler!.InvokeAsync(context).ConfigureAwait(false);
}
