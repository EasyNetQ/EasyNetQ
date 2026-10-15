namespace EasyNetQ.Pipeline;

/// <summary>
///     One step of a built pipeline. Results are carried on the context (e.g. <see cref="ConsumeContext.Ack" />),
///     so every layer shares this one delegate shape.
/// </summary>
public delegate ValueTask PipelineStep<in TContext>(TContext context) where TContext : LayerContext;

/// <summary>
///     A pipeline middleware: does its work and calls <c>next</c> (or not)
/// </summary>
public interface IMiddleware<TContext> where TContext : LayerContext
{
    /// <summary>
    ///     Processes <paramref name="context" />, delegating to <paramref name="next" /> for the rest of the pipeline
    /// </summary>
    ValueTask InvokeAsync(TContext context, PipelineStep<TContext> next);
}
