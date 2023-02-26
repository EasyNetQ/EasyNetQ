using EasyNetQ.DI;

namespace EasyNetQ.Producer;

public readonly record struct PublishContext(
    in string Exchange,
    in string RoutingKey,
    in bool Mandatory,
    in MessageProperties Properties,
    in ReadOnlyMemory<byte> Body,
    in IServiceResolver ServiceResolver,
    in CancellationToken CancellationToken
);

public delegate ValueTask PublishDelegate(PublishContext context);


public sealed class PublishPipelineBuilder
{
    private readonly IReadOnlyList<Func<PublishDelegate, PublishDelegate>> middlewares;

    public PublishPipelineBuilder()
    {
        middlewares = Array.Empty<Func<PublishDelegate, PublishDelegate>>();
    }

    private PublishPipelineBuilder(IReadOnlyList<Func<PublishDelegate, PublishDelegate>> middlewares)
    {
        this.middlewares = middlewares;
    }

    public PublishPipelineBuilder Use(Func<PublishDelegate, PublishDelegate> middleware)
    {
        // ReSharper disable once UseObjectOrCollectionInitializer
        var newMiddlewares = new List<Func<PublishDelegate, PublishDelegate>>(middlewares);
        newMiddlewares.Add(middleware);
        return new PublishPipelineBuilder(newMiddlewares);
    }

    public PublishDelegate Build()
    {
        PublishDelegate result = _ => default;
        for (var i = middlewares.Count - 1; i >= 0; i--)
            result = middlewares[i](result);
        return result;
    }
}
