using EasyNetQ.DI;

namespace EasyNetQ.Consumer;

public readonly record struct ConsumeContext(
    in MessageReceivedInfo ReceivedInfo,
    in MessageProperties Properties,
    in ReadOnlyMemory<byte> Body,
    in IServiceResolver ServiceResolver,
    in CancellationToken CancellationToken
);

public delegate ValueTask<AckStrategy> ConsumeDelegate(ConsumeContext context);

public sealed class ConsumePipelineBuilder
{
    private readonly IReadOnlyList<Func<ConsumeDelegate, ConsumeDelegate>> middlewares;

    public ConsumePipelineBuilder()
    {
        middlewares = Array.Empty<Func<ConsumeDelegate, ConsumeDelegate>>();
    }

    private ConsumePipelineBuilder(IReadOnlyList<Func<ConsumeDelegate, ConsumeDelegate>> middlewares)
    {
        this.middlewares = middlewares;
    }

    public ConsumePipelineBuilder Use(Func<ConsumeDelegate, ConsumeDelegate> middleware)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var newMiddlewares = new List<Func<ConsumeDelegate, ConsumeDelegate>>(middlewares);
        newMiddlewares.Add(middleware);
        return new ConsumePipelineBuilder(newMiddlewares);
    }

    public ConsumeDelegate Build()
    {
        ConsumeDelegate result = _ => new ValueTask<AckStrategy>(AckStrategies.NackWithRequeue);
        for (var i = middlewares.Count - 1; i >= 0; i--)
            result = middlewares[i](result);
        return result;
    }
}
