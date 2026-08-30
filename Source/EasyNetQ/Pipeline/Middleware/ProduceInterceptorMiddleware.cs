using EasyNetQ.Interception;

namespace EasyNetQ.Pipeline.Middleware;

/// <summary>
///     Runs the registered <see cref="IProduceConsumeInterceptor" />s (in registration order) over the
///     properties and body about to be published
/// </summary>
public sealed class ProduceInterceptorMiddleware : IMiddleware<PublishContext>
{
    private readonly IProduceConsumeInterceptor[] interceptors;

    /// <summary>
    ///     Creates the middleware
    /// </summary>
    public ProduceInterceptorMiddleware(IEnumerable<IProduceConsumeInterceptor> interceptors)
    {
        this.interceptors = interceptors.ToArray();
    }

    /// <inheritdoc />
    public ValueTask InvokeAsync(PublishContext context, PipelineStep<PublishContext> next)
    {
        if (interceptors.Length > 0)
        {
            var produced = interceptors.OnProduce(new ProducedMessage(context.Properties, context.Body));
            context.Properties = produced.Properties;
            context.Body = produced.Body;
        }

        return next(context);
    }
}
