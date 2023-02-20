namespace EasyNetQ.Producer;

public readonly record struct PublishContext(
    in string Exchange,
    in string RoutingKey,
    in bool Mandatory,
    in MessageProperties Properties,
    in ReadOnlyMemory<byte> Body,
    in CancellationToken CancellationToken
);

public delegate ValueTask PublishDelegate(PublishContext context);

public interface IPublishMiddleware
{
    ValueTask InvokeAsync(PublishContext context, PublishDelegate next);
}

internal static class PublishPipeline
{
    public static PublishDelegate Build(IPublishMiddleware[] middlewares, PublishDelegate publishDelegate)
    {
        var result = publishDelegate;

        for (var i = middlewares.Length - 1; i >= 0; i--)
        {
            var currentMiddleware = middlewares[i];
            var currentResult = result;

            result = ctx => currentMiddleware.InvokeAsync(ctx, currentResult);
        }

        return result;
    }
}
