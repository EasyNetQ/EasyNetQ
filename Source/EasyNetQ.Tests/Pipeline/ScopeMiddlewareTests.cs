using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.Pipeline;

public class ScopeMiddlewareTests
{
    private sealed class ScopedThing;

    [Fact]
    public async Task Should_provide_a_scope_for_the_duration_of_the_message_and_restore_afterwards()
    {
        var root = new ServiceCollection().AddScoped<ScopedThing>().BuildServiceProvider();
        var seen = new List<ScopedThing>();
        var pipeline = new PipelineBuilder<ConsumeContext>()
            .UseScope()
            .Build(root, ctx =>
            {
                ctx.Services.Should().NotBeSameAs(root);
                seen.Add(ctx.Services.GetRequiredService<ScopedThing>());
                seen.Add(ctx.Services.GetRequiredService<ScopedThing>());
                return default;
            });
        var context = new ConsumeContext(TestContexts.Consumer(services: root));

        await pipeline(context);
        await pipeline(context);

        context.Services.Should().BeSameAs(root);
        seen.Should().HaveCount(4);
        seen[0].Should().BeSameAs(seen[1], "same scope within one message");
        seen[0].Should().NotBeSameAs(seen[2], "a new scope per message");
    }
}
