using System.Collections.Concurrent;
using System.Text;
using EasyNetQ.Configuration;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class QueueScenarios
{
    /// <summary>
    ///     Typed queue settings on fluent consumers: message TTL and rejection dead-lettered through a DLX, queue
    ///     expiry, quorum queues and streams
    /// </summary>
    public static async Task QueuesAsync(ScenarioContext ctx)
    {
        const int count = 20;
        var dlx = ctx.Name("dlx");
        var dlq = ctx.Name("dlq");
        var ttlQueue = ctx.Name("ttl");
        var rejectQueue = ctx.Name("reject");
        var quorumQueue = ctx.Name("quorum");
        var streamQueue = ctx.Name("stream");
        foreach (var queue in new[] { dlq, ttlQueue, rejectQueue, quorumQueue, streamQueue })
            ctx.DeleteQueueLater(queue);
        ctx.DeleteExchangeLater(dlx);

        var deadLetters = new ConcurrentDictionary<string, int>();
        var deadLettered = new Inbox<Expiring>(ctx, e => $"{e.Kind}{e.Id}");
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var heldFirst = 0;
        var rejected = 0;
        var quorum = new Inbox<Quorumed>(ctx, q => q.Id.ToString());
        var streamed = new Inbox<Streamed>(ctx, s => s.Id.ToString());

        var bus = ctx.Bus(b => b.UseRabbitMq(r => r
            .Consume(c => c
                .Queue(dlq, q => q.Durable())
                .Bind(dlx, "#", e => e.Fanout())
                .Handle<Expiring>((message, context) =>
                {
                    if (message.Run != ctx.Run) return new ValueTask<AckDecision>(AckDecision.Ack);
                    deadLetters.AddOrUpdate(FirstDeathReason(context.Properties), 1, (_, n) => n + 1);
                    deadLettered.Add(message);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                }))
            // prefetch 1 and a handler that holds the first delivery: the rest wait in the queue and expire
            .Consume(c => c
                .Queue(ttlQueue, q => q.MessageTtl(TimeSpan.FromMilliseconds(300)).DeadLetterExchange(dlx))
                .PrefetchCount(1)
                .Handle<Expiring>(async (message, _) =>
                {
                    if (Interlocked.Exchange(ref heldFirst, 1) == 0) await gate.Task;
                    return AckDecision.Ack;
                }))
            .Consume(c => c
                .Queue(rejectQueue, q => q.DeadLetterExchange(dlx).DeadLetterRoutingKey("rejected"))
                .Handle<Expiring>((_, _) =>
                {
                    Interlocked.Increment(ref rejected);
                    return new ValueTask<AckDecision>(AckDecision.NackDiscard);
                }))
            .Consume(c => c
                .Queue(quorumQueue, q => q.Quorum())
                .Handle<Quorumed>((message, _) =>
                {
                    if (message.Run == ctx.Run) quorum.Add(message);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                }))
            .Consume(c => c
                .Queue(streamQueue, q => q.Stream())
                .PrefetchCount(100)
                .ConsumerArgument("x-stream-offset", "first")
                .Handle<Streamed>((message, _) =>
                {
                    if (message.Run == ctx.Run) streamed.Add(message);
                    return new ValueTask<AckDecision>(AckDecision.Ack);
                }))));
        var started = await Soak.CatchAsync(() => bus.StartConsumersAsync(ctx.Token));
        ctx.Check("fluent consumers with TTL/DLX, quorum and stream queues start", started is null, Soak.Describe(started));
        if (started is not null) return;

        for (var i = 0; i < count; i++)
        {
            await bus.Advanced.PublishAsync("", ttlQueue, false, null, new MessageProperties(), new Expiring(ctx.Run, i, "ttl"), ctx.Token);
            await bus.Advanced.PublishAsync("", rejectQueue, false, null, new MessageProperties(), new Expiring(ctx.Run, i, "reject"), ctx.Token);
            await bus.Advanced.PublishAsync("", quorumQueue, false, null, new MessageProperties(), new Quorumed(ctx.Run, i), ctx.Token);
            await bus.Advanced.PublishAsync("", streamQueue, false, null, new MessageProperties(), new Streamed(ctx.Run, i), ctx.Token);
        }
        ctx.CountSent(4 * count);

        // everything but the held message expires; every rejected message is dead-lettered
        var expectedDead = (count - 1) + count;
        ctx.Check("expired and rejected messages dead-lettered", await deadLettered.WaitForAsync(expectedDead, TimeSpan.FromSeconds(20), ctx.Token), $"({deadLettered.Count}/{expectedDead})");
        ctx.Check("x-first-death-reason: expired", deadLetters.GetValueOrDefault("expired") == count - 1, $"({deadLetters.GetValueOrDefault("expired")})");
        ctx.Check("x-first-death-reason: rejected", deadLetters.GetValueOrDefault("rejected") == count && Volatile.Read(ref rejected) == count, $"({deadLetters.GetValueOrDefault("rejected")}, handler saw {rejected})");
        gate.TrySetResult();

        ctx.Check("quorum queue consumed", await quorum.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({quorum.Count}/{count})");
        ctx.Check("stream consumed from the first offset", await streamed.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({streamed.Count}/{count})");
        foreach (var (queue, expected) in new[] { (quorumQueue, "quorum"), (streamQueue, "stream"), (ttlQueue, "classic") })
        {
            if (await ctx.Admin.QueueTypeAsync(queue, ctx.Token) is { } type)
                ctx.Check($"declared as {expected}", type == expected, type);
            else
                ctx.Skip($"declared as {expected}", "no management API");
        }

        // x-expires: the queue goes away once its consumer does
        var expiringQueue = ctx.Name("expires");
        ctx.DeleteQueueLater(expiringQueue);
        var expiringBus = ctx.Bus(b => b.UseRabbitMq(r => r.Consume(c => c
            .Queue(expiringQueue, q => q.Expires(TimeSpan.FromSeconds(1)))
            .Handle<Expiring>((_, _) => new ValueTask<AckDecision>(AckDecision.Ack)))));
        await expiringBus.StartConsumersAsync(ctx.Token);
        await Task.Delay(1_500, ctx.Token);
        ctx.Check("an x-expires queue stays while consumed", await ctx.Admin.QueueExistsAsync(expiringQueue, ctx.Token));
        await expiringBus.StopConsumersAsync();
        // a passive declare renews the x-expires lease: probe less often than the lease, or through management
        var gone = await Soak.WaitUntilAsync(async () =>
        {
            if (ctx.Admin.HasManagement)
                return await ctx.Admin.GetQueueAsync(expiringQueue, ctx.Token) is null;
            await Task.Delay(2_500, ctx.Token);
            return !await ctx.Admin.QueueExistsAsync(expiringQueue, ctx.Token);
        }, TimeSpan.FromSeconds(15), ctx.Token);
        ctx.Check("an x-expires queue is deleted once unused", gone);
    }

    private static string FirstDeathReason(MessageProperties properties)
        => properties.Headers?.TryGetValue("x-first-death-reason", out var value) == true
            ? value switch
            {
                byte[] bytes => Encoding.UTF8.GetString(bytes),
                string text => text,
                _ => value?.ToString() ?? "",
            }
            : "";
}
