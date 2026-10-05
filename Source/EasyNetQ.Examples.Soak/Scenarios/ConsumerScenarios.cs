using System.Collections.Concurrent;
using System.Diagnostics;
using EasyNetQ.Configuration;
using EasyNetQ.Pipeline;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class ConsumerScenarios
{
    /// <summary>
    ///     A queue deleted under its consumers: LifecycleEvent.Cancelled for a PubSub and for a fluent consumer, and
    ///     the bus keeps working afterwards
    /// </summary>
    public static async Task CancellationAsync(ScenarioContext ctx)
    {
        var fluentQueue = ctx.Name("fluent");
        ctx.DeleteQueueLater(fluentQueue);
        var events = new ConcurrentQueue<(LifecycleLayer Layer, string Event, string? Reason)>();
        var received = new Inbox<Doomed>(ctx, d => d.Id.ToString());
        var bus = ctx.Bus(b => b
            .Lifecycle(l => l.Use("record", (context, next) =>
            {
                events.Enqueue((context.Layer, context.Event.Name, context.Reason));
                return next(context);
            }))
            .UseRabbitMq(r => r.Consume(c => c
                .Queue(fluentQueue, q => q.Durable())
                .Handle<Doomed>((message, _) =>
                {
                    if (message.Run == ctx.Run) received.Add(message);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                }))));
        await bus.StartConsumersAsync(ctx.Token);

        var subscription = await bus.Bus.PubSub.SubscribeAsync<Doomed>(ctx.Name("pubsub"), (message, _) =>
        {
            if (message.Run == ctx.Run) received.Add(message);
            return Task.CompletedTask;
        }, c => { }, ctx.Token);
        var pubsubQueue = subscription.Queue.Name;
        ctx.DeleteQueueLater(pubsubQueue);

        int Count(string name) => events.Count(e => e.Layer == LifecycleLayer.Consumer && e.Event == name);
        ctx.Check("Started for the fluent and the PubSub consumer", await Soak.WaitUntilAsync(() => Count("Started") >= 2, TimeSpan.FromSeconds(10), ctx.Token), $"({Count("Started")})");

        await bus.Bus.PubSub.PublishAsync(new Doomed(ctx.Run, 1), ctx.Token);
        await bus.Advanced.PublishAsync("", fluentQueue, false, null, new MessageProperties(), new Doomed(ctx.Run, 2), ctx.Token);
        ctx.CountSent(2);
        ctx.Check("both consumers receive before the delete", await received.WaitForAsync(2, TimeSpan.FromSeconds(10), ctx.Token), $"({received.Count}/2)");

        await ctx.Admin.Advanced.QueueDeleteAsync(pubsubQueue, cancellationToken: ctx.Token);
        var pubsubCancelled = await Soak.WaitUntilAsync(() => Count("Cancelled") >= 1, TimeSpan.FromSeconds(10), ctx.Token);
        ctx.Check("deleting a PubSub queue raises Cancelled", pubsubCancelled, $"({Count("Cancelled")})");

        await ctx.Admin.Advanced.QueueDeleteAsync(fluentQueue, cancellationToken: ctx.Token);
        var fluentCancelled = await Soak.WaitUntilAsync(() => Count("Cancelled") >= 2, TimeSpan.FromSeconds(10), ctx.Token);
        ctx.Check("deleting a fluent consumer's queue raises Cancelled", fluentCancelled, $"({Count("Cancelled")})");
        ctx.Check("Cancelled carries a reason", events.Where(e => e.Event == "Cancelled").All(e => !string.IsNullOrEmpty(e.Reason)));

        var disposed = await Soak.CatchAsync(async () => await subscription.DisposeAsync());
        ctx.Check("disposing a cancelled subscription is clean", disposed is null, Soak.Describe(disposed));

        // the connection survives: a new subscription on the same bus works
        var after = new Inbox<Doomed>(ctx, d => d.Id.ToString());
        await using (await bus.Bus.PubSub.SubscribeAsync<Doomed>(ctx.Name("after"), (message, _) =>
                     {
                         if (message.Run == ctx.Run) after.Add(message);
                         return Task.CompletedTask;
                     }, c => c.WithAutoDelete(), ctx.Token))
        {
            await bus.Bus.PubSub.PublishAsync(new Doomed(ctx.Run, 3), ctx.Token);
            ctx.CountSent();
            ctx.Check("the bus consumes again after the cancellations", await after.WaitForAsync(1, TimeSpan.FromSeconds(10), ctx.Token));
        }
    }

    /// <summary>
    ///     consumerDispatcherConcurrency: 1 keeps order and runs one handler at a time; 8 runs handlers in parallel,
    ///     bounded by the prefetch count (connection-wide or per subscription)
    /// </summary>
    public static async Task ConcurrencyAsync(ScenarioContext ctx)
    {
        const int count = 200;
        var serial = await MeasureAsync(ctx, "serial", ";prefetchCount=50", null, count);
        ctx.Check("concurrency 1: in order", serial.InOrder, $"({serial.Received}/{count})");
        ctx.Check("concurrency 1: one handler at a time", serial.MaxInFlight == 1, $"(max {serial.MaxInFlight})");

        var parallel = await MeasureAsync(ctx, "parallel", ";prefetchCount=50;consumerDispatcherConcurrency=8", null, count);
        ctx.Check("concurrency 8: all received", parallel.Received == count, $"({parallel.Received}/{count})");
        ctx.Check("concurrency 8: handlers overlap, at most 8", parallel.MaxInFlight is > 1 and <= 8, $"(max {parallel.MaxInFlight})");
        ctx.Check("concurrency 8: faster than serial", parallel.Elapsed < serial.Elapsed * 0.8, $"({parallel.Elapsed.TotalMilliseconds:F0} vs {serial.Elapsed.TotalMilliseconds:F0} ms)");

        var prefetchBound = await MeasureAsync(ctx, "prefetch3", ";prefetchCount=3;consumerDispatcherConcurrency=8", null, count / 2);
        ctx.Check("prefetchCount=3 bounds concurrency 8", prefetchBound.MaxInFlight is >= 2 and <= 3 && prefetchBound.Received == count / 2, $"(max {prefetchBound.MaxInFlight}, {prefetchBound.Received}/{count / 2})");

        var perSubscription = await MeasureAsync(ctx, "sub2", ";prefetchCount=50;consumerDispatcherConcurrency=8", 2, count / 2);
        ctx.Check("WithPrefetchCount(2) bounds concurrency 8", perSubscription.MaxInFlight is >= 1 and <= 2 && perSubscription.Received == count / 2, $"(max {perSubscription.MaxInFlight}, {perSubscription.Received}/{count / 2})");
    }

    private sealed record Measurement(int Received, bool InOrder, int MaxInFlight, TimeSpan Elapsed);

    private static async Task<Measurement> MeasureAsync(ScenarioContext ctx, string name, string settings, ushort? prefetch, int count)
    {
        var bus = ctx.Bus(connection: ctx.Options.Connection + settings);
        var order = new ConcurrentQueue<int>();
        var inFlight = 0;
        var maxInFlight = 0;
        var inbox = new Inbox<Ordered>(ctx, o => o.Id.ToString());
        var queue = ctx.Name(name);
        ctx.DeleteQueueLater(queue);

        // queue first, then consume: the measurement starts with a full queue
        var declare = await bus.Bus.PubSub.SubscribeAsync<Ordered>(name, (_, _) => Task.CompletedTask, c => c.WithQueueName(queue), ctx.Token);
        await declare.DisposeAsync();
        for (var i = 0; i < count; i++)
            await bus.Bus.PubSub.PublishAsync(new Ordered(ctx.Run, i), ctx.Token);
        ctx.CountSent(count);
        await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(queue, ctx.Token) >= (ulong)count, TimeSpan.FromSeconds(10), ctx.Token);

        var watch = Stopwatch.StartNew();
        await using (await bus.Bus.PubSub.SubscribeAsync<Ordered>(name, async (message, cancellationToken) =>
                     {
                         var now = Interlocked.Increment(ref inFlight);
                         int max;
                         while (now > (max = Volatile.Read(ref maxInFlight)) && Interlocked.CompareExchange(ref maxInFlight, now, max) != max)
                         {
                         }
                         try
                         {
                             await Task.Delay(5, cancellationToken);
                             order.Enqueue(message.Id);
                             inbox.Add(message);
                         }
                         finally
                         {
                             Interlocked.Decrement(ref inFlight);
                         }
                     }, c =>
                     {
                         c.WithQueueName(queue);
                         if (prefetch is { } p) c.WithPrefetchCount(p);
                     }, ctx.Token))
        {
            await inbox.WaitForAsync(count, TimeSpan.FromSeconds(30), ctx.Token);
        }
        var ids = order.ToArray();
        return new Measurement(inbox.Count, ids.SequenceEqual(Enumerable.Range(0, count)), maxInFlight, watch.Elapsed);
    }
}
