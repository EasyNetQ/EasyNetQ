using EasyNetQ.Interception;

namespace EasyNetQ.Producer;

public sealed class PublishInterceptorsMiddleware : IPublishMiddleware
{
    private readonly IPublishConsumeInterceptor[] interceptors;

    public PublishInterceptorsMiddleware(IEnumerable<IPublishConsumeInterceptor> interceptors) => this.interceptors = interceptors.ToArray();

    public ValueTask InvokeAsync(PublishContext context, PublishDelegate next)
    {
        var producedMessage = interceptors.OnPublish(new PublishMessage(context.Properties, context.Body));
        return next(context with { Properties = producedMessage.Properties, Body = producedMessage.Body });
    }
}
