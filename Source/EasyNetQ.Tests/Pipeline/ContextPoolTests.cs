using EasyNetQ.Pipeline;

namespace EasyNetQ.Tests.Pipeline;

public class ContextPoolTests
{
    private static readonly PropertyKey<int> Marker = new("marker");

    [Fact]
    public void Should_reuse_returned_contexts_after_reset()
    {
        var consumer = TestContexts.Consumer();
        var pool = new ContextPool<ConsumeContext>(() => new ConsumeContext(consumer));

        var first = pool.Rent();
        first.Set(Marker, 1);
        first.Ack = AckDecision.NackDiscard;
        first.Body = new byte[] { 1, 2, 3 };
        pool.Return(first);

        var second = pool.Rent();
        second.Should().BeSameAs(first);
        second.TryGet(Marker, out _).Should().BeFalse();
        second.Ack.Should().Be(AckDecision.Ack);
        second.Body.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Should_not_pool_detached_contexts()
    {
        var consumer = TestContexts.Consumer();
        var pool = new ContextPool<ConsumeContext>(() => new ConsumeContext(consumer));

        var first = pool.Rent();
        first.Set(Marker, 1);
        first.Detach();
        pool.Return(first);

        first.TryGet(Marker, out _).Should().BeTrue("a detached context is not reset");
        pool.Rent().Should().NotBeSameAs(first);
    }

    [Fact]
    public void Should_retain_at_most_the_configured_number_of_contexts()
    {
        var consumer = TestContexts.Consumer();
        var created = 0;
        var pool = new ContextPool<ConsumeContext>(() =>
        {
            created++;
            return new ConsumeContext(consumer);
        }, maxRetained: 2);

        var rented = Enumerable.Range(0, 5).Select(_ => pool.Rent()).ToList();
        created.Should().Be(5);
        foreach (var context in rented)
            pool.Return(context);

        // fast slot + 2 retained
        for (var i = 0; i < 3; i++)
            pool.Rent();
        created.Should().Be(5);
        pool.Rent();
        created.Should().Be(6);
    }
}
