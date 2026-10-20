using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Configuration;

/// <summary>
///     Fluent lifecycle pipeline registration
/// </summary>
public static class EasyNetQBuilderLifecycleExtensions
{
    /// <summary>
    ///     Adds steps to the lifecycle pipeline. It runs for connection, channel and consumer events
    ///     (<see cref="LifecycleContext.Layer" /> and <see cref="LifecycleContext.Event" /> tell them apart);
    ///     with no steps registered, lifecycle notifications cost nothing.
    /// </summary>
    public static IEasyNetQBuilder Lifecycle(this IEasyNetQBuilder builder, Action<PipelineBuilder<LifecycleContext>> configure)
    {
        builder.Services.AddSingleton(new LifecycleConfiguration(configure));
        return builder;
    }
}
