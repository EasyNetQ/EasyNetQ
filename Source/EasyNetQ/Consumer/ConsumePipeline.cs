using EasyNetQ.Interception;

namespace EasyNetQ.Consumer;

public readonly record struct ConsumeContext(
    in MessageReceivedInfo ReceivedInfo,
    in MessageProperties Properties,
    in ReadOnlyMemory<byte> Body,
    in CancellationToken CancellationToken
);

public delegate ValueTask<AckStrategy> ConsumeDelegate(ConsumeContext context);

public interface IConsumeMiddleware
{
    ValueTask<AckStrategy> InvokeAsync(ConsumeContext context, ConsumeDelegate next);
}

public sealed class ConsumeInterceptorsMiddleware : IConsumeMiddleware
{
    private readonly IPublishConsumeInterceptor[] interceptors;

    public ConsumeInterceptorsMiddleware(IEnumerable<IPublishConsumeInterceptor> interceptors) => this.interceptors = interceptors.ToArray();

    public ValueTask<AckStrategy> InvokeAsync(ConsumeContext context, ConsumeDelegate next)
    {
        var consumedMessage = interceptors.OnConsume(new ConsumeMessage(context.ReceivedInfo, context.Properties, context.Body));
        return next(context with { ReceivedInfo = consumedMessage.ReceivedInfo, Properties = consumedMessage.Properties, Body = consumedMessage.Body });
    }
}

internal static class ConsumePipeline
{
    public static ConsumeDelegate Build(IConsumeMiddleware[] middlewares, ConsumeDelegate consumeDelegate)
    {
        var result = consumeDelegate;

        for (var i = middlewares.Length - 1; i >= 0; i--)
        {
            var currentMiddleware = middlewares[i];
            var currentResult = result;

            result = ctx => currentMiddleware.InvokeAsync(ctx, currentResult);
        }

        return result;
    }
}
