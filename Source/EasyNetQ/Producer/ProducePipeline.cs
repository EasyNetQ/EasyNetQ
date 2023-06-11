using EasyNetQ.DI;

namespace EasyNetQ.Producer;

public readonly record struct ProduceContext(
    in string Exchange,
    in string RoutingKey,
    in bool Mandatory,
    in bool PublisherConfirms,
    in MessageProperties Properties,
    in ReadOnlyMemory<byte> Body,
    in IServiceResolver ServiceResolver,
    in CancellationToken CancellationToken
);

public delegate ValueTask ProduceDelegate(ProduceContext context);


public sealed class ProducePipelineBuilder
{
    private readonly IReadOnlyList<Func<ProduceDelegate, ProduceDelegate>> middlewares;

    public ProducePipelineBuilder()
    {
        middlewares = Array.Empty<Func<ProduceDelegate, ProduceDelegate>>();
    }

    private ProducePipelineBuilder(IReadOnlyList<Func<ProduceDelegate, ProduceDelegate>> middlewares)
    {
        this.middlewares = middlewares;
    }

    public ProducePipelineBuilder Use(Func<ProduceDelegate, ProduceDelegate> middleware)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var newMiddlewares = new List<Func<ProduceDelegate, ProduceDelegate>>(middlewares);
        newMiddlewares.Add(middleware);
        return new ProducePipelineBuilder(newMiddlewares);
    }

    public ProduceDelegate Build()
    {
        ProduceDelegate result = _ => default;
        for (var i = middlewares.Count - 1; i >= 0; i--)
            result = middlewares[i](result);
        return result;
    }
}
