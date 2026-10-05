using System.Diagnostics;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.Pipeline;

/// <summary>
///     A fluent contribution to the lifecycle pipeline, collected by
///     <see cref="Configuration.EasyNetQBuilderLifecycleExtensions.Lifecycle" />
/// </summary>
public sealed record LifecycleConfiguration(Action<PipelineBuilder<LifecycleContext>> Configure);

/// <summary>
///     Runs the lifecycle pipeline for connection, channel and consumer events. When no step is registered,
///     notifications are free: no context is allocated and no pipeline runs.
/// </summary>
public sealed class LifecycleNotifier : IDisposable
{
    private readonly PipelineBuilder<LifecycleContext> builder;
    private readonly IServiceProvider services;
    private PipelineStep<LifecycleContext>? pipeline;
    private volatile bool disposed;

    /// <summary>
    ///     Creates the notifier, applying the fluent contributions to the pipeline builder
    /// </summary>
    public LifecycleNotifier(
        PipelineBuilder<LifecycleContext> builder,
        IEnumerable<LifecycleConfiguration> configurations,
        IServiceProvider services
    )
    {
        foreach (var configuration in configurations)
            configuration.Configure(builder);
        this.builder = builder;
        this.services = services;
    }

    /// <summary>
    ///     Whether any lifecycle step is registered. Sources wire events regardless, since every event is counted.
    /// </summary>
    public bool IsEnabled => builder.Count > 0;

    /// <summary>
    ///     Runs the lifecycle pipeline for one event under <paramref name="scope" />
    /// </summary>
    public ValueTask NotifyAsync(
        LayerContext scope,
        LifecycleLayer layer,
        LifecycleEvent @event,
        string? reason = null,
        Exception? error = null,
        CancellationToken cancellationToken = default
    )
    {
        if (disposed) return default;

        RecordMetric(layer, @event, error);
        if (!IsEnabled || ContainerDisposed()) return default;

        pipeline ??= builder.Build(services);
        var context = new LifecycleContext(scope)
        {
            Layer = layer,
            Event = @event,
            Reason = reason,
            Error = error,
            CancellationToken = cancellationToken,
        };
        return pipeline(context);
    }

    // the container disposes its services in reverse creation order, so a singleton created after this one (DefaultRpc,
    // say) stops its consumers before Dispose() below runs, while the container already refuses to resolve
    private bool ContainerDisposed()
    {
        try
        {
            services.GetService(typeof(LifecycleNotifier));
            return false;
        }
        catch (ObjectDisposedException)
        {
            disposed = true;
            return true;
        }
    }

    private static void RecordMetric(LifecycleLayer layer, LifecycleEvent @event, Exception? error)
    {
        if (!EasyNetQDiagnostics.LifecycleEvents.Enabled) return;

        var tags = new TagList
        {
            { MessagingTags.LifecycleLayer, layer switch
                {
                    LifecycleLayer.Connection => "connection",
                    LifecycleLayer.Channel => "channel",
                    LifecycleLayer.Consumer => "consumer",
                    _ => layer.ToString().ToLowerInvariant(),
                }
            },
            { MessagingTags.LifecycleEvent, @event.Name },
        };
        if (error is not null)
            tags.Add(MessagingTags.ErrorType, error.GetType().FullName);
        EasyNetQDiagnostics.LifecycleEvents.Add(1, in tags);
    }

    /// <summary>
    ///     Stops dispatching: once the container starts disposing, steps would resolve services that are gone.
    ///     Connections owned by the hosts release their lifecycle bridge themselves; this covers any other path.
    /// </summary>
    public void Dispose() => disposed = true;
}
