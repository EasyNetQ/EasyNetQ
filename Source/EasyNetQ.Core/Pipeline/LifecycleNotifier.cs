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
public sealed class LifecycleNotifier
{
    private readonly PipelineBuilder<LifecycleContext> builder;
    private readonly IServiceProvider services;
    private PipelineStep<LifecycleContext>? pipeline;

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
    ///     Whether any lifecycle step is registered; sources may skip event wiring entirely when false
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
        if (!IsEnabled) return default;

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
}
