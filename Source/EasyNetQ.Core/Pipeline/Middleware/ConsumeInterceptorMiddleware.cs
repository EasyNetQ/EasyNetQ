using EasyNetQ.Interception;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Runs the registered <see cref="IProduceConsumeInterceptor" />s (in reverse registration order) over the
///     received properties and body
/// </summary>
public sealed class ConsumeInterceptorMiddleware : IMiddleware<ConsumeContext>
{
    private readonly IProduceConsumeInterceptor[] interceptors;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ConsumeInterceptorMiddleware(IEnumerable<IProduceConsumeInterceptor> interceptors)
    {
        this.interceptors = interceptors.ToArray();
    }

    /// <inheritdoc />
    public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
    {
        if (interceptors.Length > 0)
        {
            var consumed = interceptors.OnConsume(new ConsumedMessage(context.ReceivedInfo, context.Properties, context.Body));
            context.ReceivedInfo = consumed.ReceivedInfo;
            context.Properties = consumed.Properties;
            context.Body = consumed.Body;
        }

        return next(context);
    }
}
