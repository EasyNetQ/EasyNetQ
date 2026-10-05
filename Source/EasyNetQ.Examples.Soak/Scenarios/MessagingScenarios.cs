using System.Diagnostics;
using EasyNetQ.AutoSubscribe;
using EasyNetQ.Configuration;
using EasyNetQ.Persistent;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Examples.Soak.Scenarios;

public static class MessagingScenarios
{
    /// <summary>
    ///     FuturePublish through the default DLX + TTL scheduler and, when a broker with
    ///     rabbitmq_delayed_message_exchange is configured, through UseDelayedExchangeScheduler
    /// </summary>
    public static async Task SchedulerAsync(ScenarioContext ctx)
    {
        const int count = 10;
        var delay = TimeSpan.FromSeconds(2);

        var bus = ctx.Bus();
        var inbox = new Inbox<(Scheduled Message, DateTime At)>(ctx, m => m.Message.Id.ToString());
        await using (await bus.Bus.PubSub.SubscribeAsync<Scheduled>(
                         ctx.Name("dlx"),
                         (message, _) =>
                         {
                             if (message.Run == ctx.Run) inbox.Add((message, DateTime.UtcNow));
                             return Task.CompletedTask;
                         },
                         c => c.WithAutoDelete(),
                         ctx.Token))
        {
            for (var i = 0; i < count; i++)
                await bus.Bus.Scheduler.FuturePublishAsync(new Scheduled(ctx.Run, i, DateTime.UtcNow), delay, ctx.Token);
            ctx.CountSent(count);
            await Task.Delay(delay * 0.5, ctx.Token);
            ctx.Check("DLX+TTL: nothing delivered before the delay", inbox.Count == 0, $"({inbox.Count} early)");
            ctx.Check("DLX+TTL: every future publish delivered", await inbox.WaitForAsync(count, delay + TimeSpan.FromSeconds(15), ctx.Token), $"({inbox.Count}/{count})");
            var early = inbox.Items.Count(m => m.At - m.Message.PublishedAt < delay - TimeSpan.FromMilliseconds(100));
            ctx.Check("DLX+TTL: delivered after the delay", early == 0, $"(min {inbox.Items.Select(m => (m.At - m.Message.PublishedAt).TotalMilliseconds).DefaultIfEmpty().Min():F0} ms)");
        }

        if (ctx.Options.DelayedConnection is not { } delayedConnection)
        {
            ctx.Skip("delayed exchange scheduler", "no --delayed-connection (a broker with rabbitmq_delayed_message_exchange) configured");
            return;
        }

        var delayedBus = ctx.Bus(b => b.UseDelayedExchangeScheduler(), connection: delayedConnection);
        var probe = ctx.Name("probe");
        var supported = await Soak.CatchAsync(() => delayedBus.Advanced.ExchangeDeclareAsync(
            probe, ExchangeType.DelayedMessage, arguments: new Dictionary<string, object> { [Argument.DelayedType] = ExchangeType.Direct }, cancellationToken: ctx.Token
        ));
        if (supported is not null)
        {
            ctx.Skip("delayed exchange scheduler", $"the delayed-exchange broker rejects x-delayed-message: {supported.Message}");
            return;
        }
        await delayedBus.Advanced.ExchangeDeleteAsync(probe, cancellationToken: ctx.Token);

        var delayedInbox = new Inbox<(DelayedScheduled Message, DateTime At)>(ctx, m => m.Message.Id.ToString());
        await using (await delayedBus.Bus.PubSub.SubscribeAsync<DelayedScheduled>(
                         ctx.Name("delayed"),
                         (message, _) =>
                         {
                             if (message.Run == ctx.Run) delayedInbox.Add((message, DateTime.UtcNow));
                             return Task.CompletedTask;
                         },
                         c => c.WithAutoDelete(),
                         ctx.Token))
        {
            for (var i = 0; i < count; i++)
                await delayedBus.Bus.Scheduler.FuturePublishAsync(new DelayedScheduled(ctx.Run, i, DateTime.UtcNow), delay, ctx.Token);
            ctx.CountSent(count);
            await Task.Delay(delay * 0.5, ctx.Token);
            ctx.Check("delayed exchange: nothing delivered before the delay", delayedInbox.Count == 0, $"({delayedInbox.Count} early)");
            ctx.Check("delayed exchange: every future publish delivered", await delayedInbox.WaitForAsync(count, delay + TimeSpan.FromSeconds(15), ctx.Token), $"({delayedInbox.Count}/{count})");
            var early = delayedInbox.Items.Count(m => m.At - m.Message.PublishedAt < delay - TimeSpan.FromMilliseconds(100));
            ctx.Check("delayed exchange: delivered after the delay", early == 0);
        }
    }

    /// <summary>
    ///     Request/response under concurrency, a request that outlives its expiration, a request nobody answers, and
    ///     a faulted responder (EasyNetQResponderException + a copy in the error queue)
    /// </summary>
    public static async Task RpcAsync(ScenarioContext ctx)
    {
        const int count = 40;
        var queue = ctx.Name("requests");
        var errorQueue = ctx.Name("errors");
        ctx.DeleteQueueLater(queue);
        ctx.DeleteQueueLater(errorQueue);
        ctx.DeleteExchangeLater(errorQueue);

        var bus = ctx.Bus(b => b.UseRabbitMq(r => r.ErrorQueue(errorQueue)), connection: ctx.Options.Connection + ";prefetchCount=20;consumerDispatcherConcurrency=8");

        // RabbitMQ 4 denies transient (non-durable) non-exclusive queues by default: the declare must fail fast with
        // the broker's reason instead of retrying until the timeout
        var transientWatch = Stopwatch.StartNew();
        var transient = await Soak.CatchAsync(async () =>
        {
            await using var _ = await bus.Bus.Rpc.RespondAsync<RpcRequest, RpcResponse>(
                (request, _) => Task.FromResult(new RpcResponse(request.Run, 0)), c => c.WithQueueName(ctx.Name("transient")).WithDurable(false), ctx.Token
            );
        });
        ctx.DeleteQueueLater(ctx.Name("transient"));
        if (transient is null)
            ctx.Skip("a transient responder queue the broker denies fails fast", "this broker permits transient non-exclusive queues");
        else
        {
            ctx.Check(
                "a transient responder queue the broker denies fails fast",
                transientWatch.Elapsed < TimeSpan.FromSeconds(5) && transient.Message.Contains("deprecated", StringComparison.OrdinalIgnoreCase),
                $"({transientWatch.Elapsed.TotalMilliseconds:F0} ms) {Soak.Describe(transient)}"
            );
            // the broker answers with a connection-level error: the whole connection goes down and recovers
            transientWatch.Restart();
            await Soak.WaitUntilAsync(() => !Connected(bus), TimeSpan.FromSeconds(2), ctx.Token);
            var recovered = await Soak.WaitUntilAsync(() => Connected(bus), TimeSpan.FromSeconds(60), ctx.Token);
            ctx.Check("the bus reconnects after the broker closed its connection", recovered, $"({transientWatch.Elapsed.TotalSeconds:F1} s)");
        }

        var handled = 0;
        await using var responder = await bus.Bus.Rpc.RespondAsync<RpcRequest, RpcResponse>(
            async (request, cancellationToken) =>
            {
                Interlocked.Increment(ref handled);
                if (request.Fail) throw new InvalidOperationException($"rpc failure {request.Value}");
                if (request.DelayMilliseconds > 0) await Task.Delay(request.DelayMilliseconds, cancellationToken);
                return new RpcResponse(request.Run, request.Value * 2);
            },
            c => c.WithQueueName(queue),
            ctx.Token
        );

        var responses = await Task.WhenAll(Enumerable.Range(0, count).Select(async i =>
        {
            ctx.CountSent();
            var response = await bus.Bus.Rpc.RequestAsync<RpcRequest, RpcResponse>(new RpcRequest(ctx.Run, i, 0, false), c => c.WithQueueName(queue), ctx.Token);
            ctx.CountReceived();
            return (i, response);
        }));
        ctx.Check("concurrent requests answered correctly", responses.All(r => r.response.Value == r.i * 2 && r.response.Run == ctx.Run), $"({responses.Length}/{count})");

        var watch = Stopwatch.StartNew();
        ctx.CountSent();
        var slow = await Soak.CatchAsync(() => bus.Bus.Rpc.RequestAsync<RpcRequest, RpcResponse>(
            new RpcRequest(ctx.Run, 1, 2_000, false), c => c.WithQueueName(queue).WithExpiration(TimeSpan.FromMilliseconds(400)), ctx.Token
        ));
        var slowElapsed = watch.Elapsed;
        ctx.Check("a request outliving its expiration fails as cancelled/timeout", slow is OperationCanceledException or TimeoutException, Soak.Describe(slow));
        ctx.Check("... and fails at the expiration, not at the response", slowElapsed < TimeSpan.FromMilliseconds(1_500), $"({slowElapsed.TotalMilliseconds:F0} ms)");

        ctx.CountSent();
        var after = await bus.Bus.Rpc.RequestAsync<RpcRequest, RpcResponse>(new RpcRequest(ctx.Run, 21, 0, false), c => c.WithQueueName(queue), ctx.Token);
        ctx.CountReceived();
        ctx.Check("requests keep working after a timed-out one (late response ignored)", after.Value == 42);

        watch.Restart();
        ctx.CountSent();
        var unanswered = await Soak.CatchAsync(() => bus.Bus.Rpc.RequestAsync<RpcRequest, RpcResponse>(
            new RpcRequest(ctx.Run, 1, 0, false), c => c.WithQueueName(ctx.Name("nobody")).WithExpiration(TimeSpan.FromMilliseconds(500)), ctx.Token
        ));
        ctx.Check("a request nobody answers expires", unanswered is OperationCanceledException or TimeoutException && watch.Elapsed < TimeSpan.FromSeconds(3), $"{Soak.Describe(unanswered)} after {watch.Elapsed.TotalMilliseconds:F0} ms");

        ctx.CountSent();
        var faulted = await Soak.CatchAsync(() => bus.Bus.Rpc.RequestAsync<RpcRequest, RpcResponse>(new RpcRequest(ctx.Run, 7, 0, true), c => c.WithQueueName(queue), ctx.Token));
        ctx.Check("a faulted responder surfaces as EasyNetQResponderException with its message", faulted is EasyNetQResponderException && faulted.Message.Contains("rpc failure 7", StringComparison.Ordinal), Soak.Describe(faulted));
        ctx.Check("the faulted request is copied to the error queue", await Soak.WaitUntilAsync(async () => await ctx.Admin.MessageCountAsync(errorQueue, ctx.Token) >= 1, TimeSpan.FromSeconds(10), ctx.Token));
    }

    private static bool Connected(SoakBus bus)
        => bus.Advanced.GetConnectionStatus(PersistentConnectionType.Producer).State == PersistentConnectionState.Connected
           && bus.Advanced.GetConnectionStatus(PersistentConnectionType.Consumer).State == PersistentConnectionState.Connected;

    /// <summary>
    ///     SendReceive with two message types on one queue
    /// </summary>
    public static async Task SendReceiveAsync(ScenarioContext ctx)
    {
        const int count = 40;
        var queue = ctx.Name("commands");
        ctx.DeleteQueueLater(queue);
        var bus = ctx.Bus();
        var a = new Inbox<CommandA>(ctx, m => m.Id.ToString());
        var b = new Inbox<CommandB>(ctx, m => m.Id.ToString());
        await using var receiver = await bus.Bus.SendReceive.ReceiveAsync(queue, r => r
            .Add<CommandA>(message => a.Add(message))
            .Add<CommandB>(message => b.Add(message)), ctx.Token);

        for (var i = 0; i < count; i++)
        {
            await bus.Bus.SendReceive.SendAsync(queue, new CommandA(ctx.Run, i), ctx.Token);
            if (i % 2 == 0) await bus.Bus.SendReceive.SendAsync(queue, new CommandB(ctx.Run, i), ctx.Token);
        }
        ctx.CountSent(count + count / 2);
        ctx.Check("CommandA received", await a.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({a.Count}/{count})");
        ctx.Check("CommandB received", await b.WaitForAsync(count / 2, TimeSpan.FromSeconds(20), ctx.Token), $"({b.Count}/{count / 2})");
        ctx.Check("no duplicates", a.Duplicates == 0 && b.Duplicates == 0);
    }

    /// <summary>
    ///     The reflection AutoSubscriber: IConsume and IConsumeAsync, [ForTopic] filtering, [SubscriptionConfiguration]
    ///     on the method and on the class, and [AutoSubscriberConsumer] subscription ids
    /// </summary>
    public static async Task AutoSubscriberAsync(ScenarioContext ctx)
    {
        const int count = 20;
        var sink = new AutoSubscriberSink(ctx);
        var bus = ctx.Bus(services: s => s
            .AddSingleton(sink)
            .AddTransient<TopicConsumer>()
            .AddTransient<SyncConsumer>()
            .AddTransient<AsyncConsumer>());

        var subscriber = new AutoSubscriber(bus.Bus, bus.Provider, ctx.Name("auto"));
        var subscriptions = await subscriber.SubscribeAsync([typeof(TopicConsumer), typeof(SyncConsumer), typeof(AsyncConsumer)], ctx.Token);
        for (var i = 0; i < count; i++)
        {
            await bus.Bus.PubSub.PublishAsync(new TopicEvent(ctx.Run, i), $"soak.red.{i}", ctx.Token);
            await bus.Bus.PubSub.PublishAsync(new TopicEvent(ctx.Run, 1000 + i), $"soak.blue.{i}.x", ctx.Token);
            await bus.Bus.PubSub.PublishAsync(new TopicEvent(ctx.Run, 2000 + i), $"soak.green.{i}", ctx.Token);
            await bus.Bus.PubSub.PublishAsync(new SyncEvent(ctx.Run, i), ctx.Token);
            await bus.Bus.PubSub.PublishAsync(new AsyncEvent(ctx.Run, i), ctx.Token);
        }
        ctx.CountSent(5 * count);

        ctx.Check("IConsume<T> with [ForTopic] x2 receives the matching topics", await sink.Topic.WaitForAsync(2 * count, TimeSpan.FromSeconds(20), ctx.Token), $"({sink.Topic.Count}/{2 * count})");
        await Task.Delay(300, ctx.Token);
        ctx.Check("[ForTopic] filters other topics", sink.Topic.Items.All(e => e.Id < 2000), $"({sink.Topic.Items.Count(e => e.Id >= 2000)} unmatched delivered)");
        ctx.Check("IConsume<T> receives", await sink.Sync.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({sink.Sync.Count}/{count})");
        ctx.Check("IConsumeAsync<T> with [AutoSubscriberConsumer] receives", await sink.Async.WaitForAsync(count, TimeSpan.FromSeconds(20), ctx.Token), $"({sink.Async.Count}/{count})");
        ctx.Check("consumers resolved from the container per message", sink.Instances >= 3 * count, $"({sink.Instances} instances)");

        var conventions = bus.Provider.GetRequiredService<IConventions>();
        var asyncQueue = conventions.QueueNamingConvention(typeof(AsyncEvent), AsyncConsumer.SubscriptionId);
        var syncQueue = conventions.QueueNamingConvention(typeof(SyncEvent), subscriberIdFor(subscriber, typeof(SyncConsumer), typeof(SyncEvent)));
        var topicQueue = conventions.QueueNamingConvention(typeof(TopicEvent), subscriberIdFor(subscriber, typeof(TopicConsumer), typeof(TopicEvent)));
        ctx.Check("[AutoSubscriberConsumer] subscription id names the queue", await ctx.Admin.QueueExistsAsync(asyncQueue, ctx.Token), asyncQueue);
        ctx.DeleteQueueLater(asyncQueue);
        ctx.DeleteQueueLater(syncQueue);
        ctx.DeleteQueueLater(topicQueue);

        if (await ctx.Admin.GetQueueAsync(syncQueue, ctx.Token) is { } syncJson)
            ctx.Check("[SubscriptionConfiguration] on the method: auto-delete queue", syncJson.GetProperty("auto_delete").GetBoolean());
        else
            ctx.Skip("[SubscriptionConfiguration] on the method: auto-delete queue", "no management API");

        // 8.x and v9.0.0-alpha.1 read the attribute from the method only, although it is allowed on the class; the
        // AOT AutoSubscriber work (AutoSubscriberConsumerInfo.SubscriptionConfiguration) adds the class fallback
        const string classLevel = "[SubscriptionConfiguration] on the class: auto-delete queue";
        if (typeof(AutoSubscriberConsumerInfo).GetProperty("SubscriptionConfiguration") is null)
            ctx.Skip(classLevel, "known gap: class-level attribute ignored until the AOT AutoSubscriber lands");
        else if (await ctx.Admin.GetQueueAsync(topicQueue, ctx.Token) is { } topicJson)
            ctx.Check(classLevel, topicJson.GetProperty("auto_delete").GetBoolean());
        else
            ctx.Skip(classLevel, "no management API");

        await subscriptions.DisposeAsync();

        static string subscriberIdFor(AutoSubscriber subscriber, Type consumer, Type message)
            => new SubscriptionIds(subscriber).For(new AutoSubscriberConsumerInfo(consumer, typeof(IConsume<>).MakeGenericType(message), message));
    }

    private sealed class SubscriptionIds(AutoSubscriber subscriber) : AutoSubscriber(null!, null!, subscriber.SubscriptionIdPrefix)
    {
        public string For(AutoSubscriberConsumerInfo info) => DefaultSubscriptionIdGenerator(info);
    }
}

public sealed class AutoSubscriberSink(ScenarioContext ctx)
{
    private int instances;
    private int syncInFlight;
    private int maxSyncInFlight;

    public Inbox<TopicEvent> Topic { get; } = new(ctx, e => e.Id.ToString());
    public Inbox<SyncEvent> Sync { get; } = new(ctx, e => e.Id.ToString());
    public Inbox<AsyncEvent> Async { get; } = new(ctx, e => e.Id.ToString());
    public string Run => ctx.Run;
    public int Instances => Volatile.Read(ref instances);
    public int MaxSyncInFlight => Volatile.Read(ref maxSyncInFlight);

    public void Created() => Interlocked.Increment(ref instances);

    public IDisposable EnterSync()
    {
        var now = Interlocked.Increment(ref syncInFlight);
        int max;
        while (now > (max = Volatile.Read(ref maxSyncInFlight)) && Interlocked.CompareExchange(ref maxSyncInFlight, now, max) != max)
        {
        }
        return new Exit(() => Interlocked.Decrement(ref syncInFlight));
    }

    private sealed class Exit(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}

[SubscriptionConfiguration(AutoDelete = true)]
public sealed class TopicConsumer : IConsume<TopicEvent>
{
    private readonly AutoSubscriberSink sink;

    public TopicConsumer(AutoSubscriberSink sink)
    {
        this.sink = sink;
        sink.Created();
    }

    [ForTopic("soak.red.*")]
    [ForTopic("soak.blue.#")]
    public void Consume(TopicEvent message, CancellationToken cancellationToken = default)
    {
        if (message.Run == sink.Run) sink.Topic.Add(message);
    }
}

public sealed class SyncConsumer : IConsume<SyncEvent>
{
    private readonly AutoSubscriberSink sink;

    public SyncConsumer(AutoSubscriberSink sink)
    {
        this.sink = sink;
        sink.Created();
    }

    [SubscriptionConfiguration(AutoDelete = true, PrefetchCount = 3)]
    public void Consume(SyncEvent message, CancellationToken cancellationToken = default)
    {
        using var _ = sink.EnterSync();
        if (message.Run == sink.Run) sink.Sync.Add(message);
    }
}

public sealed class AsyncConsumer : IConsumeAsync<AsyncEvent>
{
    public const string SubscriptionId = "soak-autosubscriber-fixed";
    private readonly AutoSubscriberSink sink;

    public AsyncConsumer(AutoSubscriberSink sink)
    {
        this.sink = sink;
        sink.Created();
    }

    [AutoSubscriberConsumer(SubscriptionId = SubscriptionId)]
    [SubscriptionConfiguration(AutoDelete = true)]
    public async Task ConsumeAsync(AsyncEvent message, CancellationToken cancellationToken = default)
    {
        await Task.Yield();
        if (message.Run == sink.Run) sink.Async.Add(message);
    }
}
