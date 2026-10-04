using EasyNetQ.Consumer;
using EasyNetQ.Pipeline;
using EasyNetQ.Pipeline.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasyNetQ.Tests.Pipeline;

public class ErrorHandlingMiddlewareTests
{
    private readonly IConsumeErrorStrategy strategy = Substitute.For<IConsumeErrorStrategy>();
    private readonly IServiceProvider services = new ServiceCollection().BuildServiceProvider();

    private PipelineStep<ConsumeContext> Build(PipelineStep<ConsumeContext> terminal)
        => new PipelineBuilder<ConsumeContext>()
            .Use(new ErrorHandlingMiddleware(strategy, NullLogger<ErrorHandlingMiddleware>.Instance))
            .Build(services, terminal);

    [Fact]
    public async Task Should_pass_through_when_nothing_fails()
    {
        var pipeline = Build(ctx =>
        {
            ctx.Ack = AckDecision.NackDiscard;
            return default;
        });
        var context = new ConsumeContext(TestContexts.Consumer());

        await pipeline(context);

        context.Ack.Should().Be(AckDecision.NackDiscard);
        context.Error.Should().BeNull();
        await strategy.DidNotReceiveWithAnyArgs().HandleErrorAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_ask_the_strategy_on_exception()
    {
        var exception = new InvalidOperationException("boom");
        strategy.HandleErrorAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new ValueTask<AckDecision>(AckDecision.NackRequeue));
        var pipeline = Build(_ => throw exception);
        var context = new ConsumeContext(TestContexts.Consumer());

        await pipeline(context);

        context.Ack.Should().Be(AckDecision.NackRequeue);
        context.Error.Should().BeSameAs(exception);
        await strategy.Received().HandleErrorAsync(context, exception, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_ask_the_strategy_on_cancellation_when_the_consumer_is_stopping()
    {
        using var cts = new CancellationTokenSource();
        strategy.HandleCancelledAsync(default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new ValueTask<AckDecision>(AckDecision.NackRequeue));
        var pipeline = Build(_ =>
        {
            cts.Cancel();
            throw new OperationCanceledException(cts.Token);
        });
        var context = new ConsumeContext(TestContexts.Consumer()) { CancellationToken = cts.Token };

        await pipeline(context);

        context.Ack.Should().Be(AckDecision.NackRequeue);
        await strategy.Received().HandleCancelledAsync(context, Arg.Any<CancellationToken>());
        await strategy.DidNotReceiveWithAnyArgs().HandleErrorAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Should_treat_cancellation_without_consumer_stop_as_an_error()
    {
        strategy.HandleErrorAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new ValueTask<AckDecision>(AckDecision.Ack));
        var pipeline = Build(_ => throw new OperationCanceledException());
        var context = new ConsumeContext(TestContexts.Consumer());

        await pipeline(context);

        await strategy.Received().HandleErrorAsync(context, Arg.Any<OperationCanceledException>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Should_nack_with_requeue_when_the_strategy_itself_fails()
    {
        strategy.HandleErrorAsync(default!, default!, TestContext.Current.CancellationToken).ReturnsForAnyArgs(new ValueTask<AckDecision>(Task.FromException<AckDecision>(new Exception("strategy failed"))));
        var pipeline = Build(_ => throw new Exception("handler failed"));
        var context = new ConsumeContext(TestContexts.Consumer());

        await pipeline(context);

        context.Ack.Should().Be(AckDecision.NackRequeue);
    }
}
