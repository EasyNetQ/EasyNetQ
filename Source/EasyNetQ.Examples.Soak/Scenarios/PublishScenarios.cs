using System.Diagnostics;
using System.Text;
using EasyNetQ.Configuration;
using EasyNetQ.Producer;
using EasyNetQ.Topology;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class PublishScenarios
{
    /// <summary>
    ///     x-max-priority queues: messages queued while nobody consumes come out highest priority first, through
    ///     PubSub (WithMaxPriority/WithPriority) and through a fluent typed queue (MaxPriority)
    /// </summary>
    public static async Task PriorityAsync(ScenarioContext ctx)
    {
        const int count = 60;
        var bus = ctx.Bus();

        // PubSub: declare the queue with a first subscription, stop consuming, queue messages, consume again
        var subscriptionId = ctx.Name("pubsub");
        var first = await bus.Bus.PubSub.SubscribeAsync<Prioritized>(subscriptionId, (_, _) => Task.CompletedTask, c => c.WithMaxPriority(9), ctx.Token);
        var queue = first.Queue.Name;
        ctx.DeleteQueueLater(queue);
        await first.DisposeAsync();

        for (var i = 0; i < count; i++)
        {
            var priority = (byte)(i * 7 % 10);
            await bus.Bus.PubSub.PublishAsync(new Prioritized(ctx.Run, i, priority), c => c.WithPriority(priority), ctx.Token);
        }
        ctx.CountSent(count);
        ctx.Check("PubSub messages queued", await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(queue, ctx.Token) >= count, TimeSpan.FromSeconds(10), ctx.Token));

        var inbox = new Inbox<Prioritized>(ctx, p => p.Id.ToString());
        await using (await bus.Bus.PubSub.SubscribeAsync<Prioritized>(
                         subscriptionId, (message, _) =>
                         {
                             inbox.Add(message);
                             return Task.CompletedTask;
                         }, c => c.WithMaxPriority(9).WithPrefetchCount(1), ctx.Token))
        {
            ctx.Check("PubSub priority messages received", await inbox.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({inbox.Count}/{count})");
        }
        var priorities = inbox.Items.Select(p => p.Priority).ToList();
        ctx.Check("PubSub delivery is highest priority first", IsNonIncreasing(priorities), $"({string.Join("", priorities.Take(20))}...)");

        // fluent: the typed queue builder declares x-max-priority; the queue is pre-declared through the advanced API
        // with the same setting, as an app that owns the queue elsewhere would
        var fluentQueue = ctx.Name("fluent");
        ctx.DeleteQueueLater(fluentQueue);
        var declared = await Soak.CatchAsync(() => bus.Advanced.QueueDeclareAsync(fluentQueue, c => c.WithMaxPriority(9), ctx.Token));
        ctx.Check("advanced API declares the priority queue", declared is null, Soak.Describe(declared));
        for (var i = 0; i < count; i++)
        {
            var priority = (byte)(i * 3 % 10);
            var body = Encoding.UTF8.GetBytes($"{{\"Run\":\"{ctx.Run}\",\"Id\":{i},\"Priority\":{priority}}}");
            await bus.Advanced.PublishAsync("", fluentQueue, false, null, new MessageProperties { Priority = priority }, (ReadOnlyMemory<byte>)body, ctx.Token);
        }
        ctx.CountSent(count);
        await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(fluentQueue, ctx.Token) >= count, TimeSpan.FromSeconds(10), ctx.Token);

        var fluentInbox = new Inbox<FluentPrioritized>(ctx, p => p.Id.ToString());
        var fluentBus = ctx.Bus(b => b.UseRabbitMq(r => r.Consume(c => c
            .Queue(fluentQueue, q => q.MaxPriority(9))
            .PrefetchCount(1)
            .Handle<FluentPrioritized>((message, _) =>
            {
                fluentInbox.Add(message);
                return new ValueTask<AckDecision>(AckDecision.Ack);
            }))));
        var started = await Soak.CatchAsync(() => fluentBus.StartConsumersAsync(ctx.Token));
        ctx.Check("fluent MaxPriority matches the queue the advanced API declared", started is null, Soak.Describe(started));
        if (started is not null) return;
        ctx.Check("fluent priority messages received", await fluentInbox.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({fluentInbox.Count}/{count})");
        var fluentPriorities = fluentInbox.Items.Select(p => p.Priority).ToList();
        ctx.Check("fluent delivery is highest priority first", IsNonIncreasing(fluentPriorities), $"({string.Join("", fluentPriorities.Take(20))}...)");
    }

    /// <summary>
    ///     UseMultiChannelClientCommandDispatcher with confirms under concurrent publishers: nothing lost, nothing doubled
    /// </summary>
    public static async Task MultiChannelAsync(ScenarioContext ctx)
    {
        const int producers = 8, perProducer = 250;
        var bus = ctx.Bus(b => b.UseMultiChannelClientCommandDispatcher(4), connection: ctx.Options.Connection + ";publisherConfirms=true;consumerDispatcherConcurrency=4;prefetchCount=100");
        var inbox = new Inbox<Tick>(ctx, t => $"{t.Producer}.{t.Id}");
        await using var subscription = await bus.Bus.PubSub.SubscribeAsync<Tick>(
            ctx.Name("ticks"),
            (tick, _) =>
            {
                if (tick.Run == ctx.Run) inbox.Add(tick);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        var watch = Stopwatch.StartNew();
        var failures = 0;
        await Task.WhenAll(Enumerable.Range(0, producers).Select(producer => Task.Run(async () =>
        {
            for (var i = 0; i < perProducer; i++)
            {
                try
                {
                    await bus.Bus.PubSub.PublishAsync(new Tick(ctx.Run, producer, i), ctx.Token);
                    ctx.CountSent();
                }
                catch (Exception) when (!ctx.Token.IsCancellationRequested)
                {
                    Interlocked.Increment(ref failures);
                }
            }
        }, ctx.Token)));
        var publishElapsed = watch.Elapsed;

        const int total = producers * perProducer;
        ctx.Check("every confirmed publish succeeded", failures == 0, $"({failures} failed, {total / Math.Max(publishElapsed.TotalSeconds, 0.001):F0} msg/s)");
        ctx.Check("every message received", await inbox.WaitForAsync(total, TimeSpan.FromSeconds(30), ctx.Token), $"({inbox.Count}/{total})");
        await Task.Delay(200, ctx.Token);
        ctx.Check("no duplicates", inbox.Duplicates == 0, $"({inbox.Duplicates})");
    }

    /// <summary>
    ///     Publisher confirms on/off (connection-wide and per request), mandatory publishes that nobody routes
    ///     (PublishReturnedException / UnroutableMessageException / MessageReturned), and recovery after a publish to
    ///     a missing exchange
    /// </summary>
    public static async Task ConfirmsAsync(ScenarioContext ctx)
    {
        const int count = 50;
        var voidExchange = ctx.Name("void");
        var fluentVoid = ctx.Name("fluent-void");
        ctx.DeleteExchangeLater(voidExchange);
        ctx.DeleteExchangeLater(fluentVoid);

        var confirming = ctx.Bus(b => b.UseRabbitMq(r => r.Publish(p => p
                .Exchange(fluentVoid, e => e.Direct())
                .Mandatory()
                .PublisherConfirms()
                .Message<Unroutable>("nowhere"))),
            connection: ctx.Options.Connection + ";publisherConfirms=true");
        var plain = ctx.Bus(connection: ctx.Options.Connection + ";publisherConfirms=false");

        var inbox = new Inbox<Confirmed>(ctx, c => c.Id.ToString());
        await using var subscription = await plain.Bus.PubSub.SubscribeAsync<Confirmed>(
            ctx.Name("confirmed"),
            (message, _) =>
            {
                if (message.Run == ctx.Run) inbox.Add(message);
                return Task.CompletedTask;
            },
            c => c.WithAutoDelete(),
            ctx.Token
        );

        for (var i = 0; i < count; i++)
            await confirming.Bus.PubSub.PublishAsync(new Confirmed(ctx.Run, i), ctx.Token);
        for (var i = 0; i < count; i++)
            await plain.Bus.PubSub.PublishAsync(new Confirmed(ctx.Run, count + i), ctx.Token);
        for (var i = 0; i < count; i++)
            await plain.Bus.PubSub.PublishAsync(new Confirmed(ctx.Run, 2 * count + i), c => c.WithPublisherConfirms(true), ctx.Token);
        ctx.CountSent(3 * count);
        ctx.Check("confirmed, unconfirmed and per-request-confirmed publishes received", await inbox.WaitForAsync(3 * count, TimeSpan.FromSeconds(20), ctx.Token), $"({inbox.Count}/{3 * count})");

        await confirming.Advanced.ExchangeDeclareAsync(voidExchange, ExchangeType.Direct, cancellationToken: ctx.Token);
        var body = (ReadOnlyMemory<byte>)Encoding.UTF8.GetBytes("{}");
        var returned = await Soak.CatchAsync(() => confirming.Advanced.PublishAsync(voidExchange, "nowhere", true, true, new MessageProperties(), body, ctx.Token));
        ctx.Check("mandatory + confirms, no route: PublishReturnedException", returned is PublishReturnedException, Soak.Describe(returned));
        ctx.Check("PublishReturnedException is an UnroutableMessageException", returned is UnroutableMessageException);

        var returnedEvent = new TaskCompletionSource<MessageReturnedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<MessageReturnedEventArgs> onReturned = (_, args) =>
        {
            if (args.MessageReturnedInfo.Exchange == voidExchange) returnedEvent.TrySetResult(args);
        };
        plain.Advanced.MessageReturned += onReturned;
        try
        {
            var unconfirmed = await Soak.CatchAsync(() => plain.Advanced.PublishAsync(voidExchange, "nowhere", true, false, new MessageProperties(), body, ctx.Token));
            ctx.Check("mandatory without confirms does not throw", unconfirmed is null, Soak.Describe(unconfirmed));
            var raised = await Task.WhenAny(returnedEvent.Task, Task.Delay(TimeSpan.FromSeconds(5), ctx.Token)) == returnedEvent.Task;
            ctx.Check("mandatory without confirms raises MessageReturned", raised, raised ? $"({returnedEvent.Task.Result.MessageReturnedInfo.RoutingKey} {returnedEvent.Task.Result.MessageReturnedInfo.ReturnReason})" : "");
        }
        finally
        {
            plain.Advanced.MessageReturned -= onReturned;
        }

        var fluent = await Soak.CatchAsync(() => confirming.Publisher.PublishAsync(new Unroutable(ctx.Run, 1), ctx.Token).AsTask());
        ctx.Check("fluent Mandatory().PublisherConfirms() route without a binding: UnroutableMessageException", fluent is UnroutableMessageException, Soak.Describe(fluent));

        var missing = await Soak.CatchAsync(() => confirming.Advanced.PublishAsync(ctx.Name("missing-exchange"), "x", false, true, new MessageProperties(), body, ctx.Token));
        ctx.Check("confirmed publish to a missing exchange fails", missing is not null, Soak.Describe(missing));
        var afterwards = new List<Exception>();
        for (var i = 0; i < 10; i++)
        {
            if (await Soak.CatchAsync(() => confirming.Bus.PubSub.PublishAsync(new Confirmed(ctx.Run, 3 * count + i), ctx.Token)) is { } failure)
                afterwards.Add(failure);
            else
                ctx.CountSent();
        }
        ctx.Check("publishing recovers after the channel error", afterwards.Count == 0, afterwards.Count == 0 ? "" : Soak.Describe(afterwards[0]));
        ctx.Check("publishes after recovery received", await inbox.WaitForAsync(3 * count + 10 - afterwards.Count, TimeSpan.FromSeconds(20), ctx.Token), $"({inbox.Count}/{3 * count + 10 - afterwards.Count})");
    }

    private static bool IsNonIncreasing(IReadOnlyList<byte> values)
    {
        for (var i = 1; i < values.Count; i++)
            if (values[i] > values[i - 1])
                return false;
        return true;
    }
}
