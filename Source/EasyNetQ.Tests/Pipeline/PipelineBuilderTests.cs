using EasyNetQ.Pipeline;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.Pipeline;

public class PipelineBuilderTests
{
    private static readonly PropertyKey<List<string>> Trace = new("trace");

    private static ConsumeContext NewContext(IServiceProvider? services = null)
    {
        var context = new ConsumeContext(TestContexts.Consumer(services: services));
        context.Set(Trace, new List<string>());
        return context;
    }

    private sealed class Step : IMiddleware<ConsumeContext>
    {
        private readonly string name;
        public Step(string name) => this.name = name;

        public async ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
        {
            context.Get(Trace).Add($"{name}:before");
            await next(context);
            context.Get(Trace).Add($"{name}:after");
        }
    }

    private sealed class A : IMiddleware<ConsumeContext>
    {
        public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
        {
            context.Get(Trace).Add("A");
            return next(context);
        }
    }

    private sealed class B : IMiddleware<ConsumeContext>
    {
        public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
        {
            context.Get(Trace).Add("B");
            return next(context);
        }
    }

    private sealed class ShortCircuit : IMiddleware<ConsumeContext>
    {
        public ValueTask InvokeAsync(ConsumeContext context, PipelineStep<ConsumeContext> next)
        {
            context.Get(Trace).Add("stop");
            context.Ack = AckDecision.NackDiscard;
            return default;
        }
    }

    [Fact]
    public async Task Should_run_steps_in_registration_order_around_the_terminal()
    {
        var pipeline = new PipelineBuilder<ConsumeContext>()
            .Use(new Step("1"))
            .Use(new Step("2"))
            .Build(new ServiceCollection().BuildServiceProvider(), ctx =>
            {
                ctx.Get(Trace).Add("terminal");
                ctx.Ack = AckDecision.NackRequeue;
                return default;
            });

        var context = NewContext();
        await pipeline(context);

        context.Get(Trace).Should().Equal("1:before", "2:before", "terminal", "2:after", "1:after");
        context.Ack.Should().Be(AckDecision.NackRequeue);
    }

    [Fact]
    public async Task Should_support_replace_insert_and_remove_by_marker_type()
    {
        var builder = new PipelineBuilder<ConsumeContext>().Use(new A()).Use(new B());
        builder.Steps.Should().Equal("A", "B");

        builder.InsertBefore<B>(new Step("x")).InsertAfter<B>(new Step("y")).Replace<A>(new Step("a"));
        builder.Steps.Should().Equal("Step", "Step", "B", "Step");

        builder.Remove<B>();
        builder.Steps.Should().Equal("Step", "Step", "Step");

        var context = NewContext();
        await builder.Build(new ServiceCollection().BuildServiceProvider())(context);
        context.Get(Trace).Should().Equal("a:before", "x:before", "y:before", "y:after", "x:after", "a:after");
    }

    [Fact]
    public void Should_throw_for_unknown_markers()
    {
        var builder = new PipelineBuilder<ConsumeContext>().Use(new A());

        var act = () => builder.Remove<B>();
        act.Should().Throw<InvalidOperationException>().WithMessage("*B*");
    }

    [Fact]
    public async Task Should_support_inline_steps_by_name()
    {
        var builder = new PipelineBuilder<ConsumeContext>()
            .Use("first", (ctx, next) =>
            {
                ctx.Get(Trace).Add("first");
                return next(ctx);
            })
            .Use("second", (ctx, next) =>
            {
                ctx.Get(Trace).Add("second");
                return next(ctx);
            });

        builder.Remove("first");
        builder.Steps.Should().Equal("second");

        var context = NewContext();
        await builder.Build(new ServiceCollection().BuildServiceProvider())(context);
        context.Get(Trace).Should().Equal("second");
    }

    [Fact]
    public async Task Clone_should_isolate_additions()
    {
        var parent = new PipelineBuilder<ConsumeContext>().Use(new A());
        var child = parent.Clone().Use(new B());

        parent.Steps.Should().Equal("A");
        child.Steps.Should().Equal("A", "B");

        var context = NewContext();
        await child.Build(new ServiceCollection().BuildServiceProvider())(context);
        context.Get(Trace).Should().Equal("A", "B");
    }

    [Fact]
    public async Task Should_resolve_middleware_from_services_at_build_time()
    {
        var services = new ServiceCollection().AddSingleton<A>().BuildServiceProvider();
        var pipeline = new PipelineBuilder<ConsumeContext>().Use<A>().Use(_ => new B()).Build(services);

        var context = NewContext();
        await pipeline(context);
        context.Get(Trace).Should().Equal("A", "B");
    }

    [Fact]
    public async Task Middleware_should_be_able_to_short_circuit()
    {
        var pipeline = new PipelineBuilder<ConsumeContext>()
            .Use(new A())
            .Use(new ShortCircuit())
            .Use(new B())
            .Build(new ServiceCollection().BuildServiceProvider(), ctx =>
            {
                ctx.Get(Trace).Add("terminal");
                return default;
            });

        var context = NewContext();
        await pipeline(context);
        context.Get(Trace).Should().Equal("A", "stop");
        context.Ack.Should().Be(AckDecision.NackDiscard);
    }
}
