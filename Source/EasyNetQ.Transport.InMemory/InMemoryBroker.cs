using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace EasyNetQ.Transport.InMemory;

/// <summary>
///     One message routed to a queue
/// </summary>
public sealed record InMemoryDelivery(string Exchange, string RoutingKey, MessageProperties Properties, byte[] Body, bool Redelivered = false)
{
    // set while a TTL runs: 0 = waiting, 1 = taken by a consumer, 2 = expired
    internal StrongBox<int>? Expiry { get; init; }

    internal bool TryTake() => Expiry is null || Interlocked.CompareExchange(ref Expiry.Value, 1, 0) == 0;
}

/// <summary>
///     The in-process broker: exchanges, queues, bindings, AMQP-style topic matching. The default exchange
///     ("") routes the routing key straight to the queue of that name. Message TTL (<c>x-message-ttl</c> and the
///     per-message expiration) and dead-lettering of expired messages (<c>x-dead-letter-exchange</c>,
///     <c>x-dead-letter-routing-key</c>) work as on RabbitMQ, which is what the DLX + TTL scheduler relies on.
/// </summary>
public sealed class InMemoryBroker
{
    internal sealed class InMemoryExchange(string type)
    {
        public string Type { get; } = type;
        // copy-on-write: routing reads a stable snapshot while bindings change concurrently
        public volatile BindingDefinition[] Bindings = [];
    }

    internal sealed class InMemoryQueue(string name, IDictionary<string, object>? arguments)
    {
        public string Name { get; } = name;
        public Channel<InMemoryDelivery> Deliveries { get; } = Channel.CreateUnbounded<InMemoryDelivery>();
        public int ConsumerCount;
        // expired deliveries still in the channel, skipped when read
        public int Expired;
        public TimeSpan? MessageTtl { get; } = arguments?.TryGetValue(Argument.MessageTtl, out var ttl) == true
            ? TimeSpan.FromMilliseconds(Convert.ToDouble(ttl, CultureInfo.InvariantCulture))
            : null;
        public string? DeadLetterExchange { get; } = Text(arguments, Argument.DeadLetterExchange);
        public string? DeadLetterRoutingKey { get; } = Text(arguments, Argument.DeadLetterRoutingKey);

        public int Count => Deliveries.Reader.Count - Volatile.Read(ref Expired);

        private static string? Text(IDictionary<string, object>? arguments, string key)
            => arguments?.TryGetValue(key, out var value) == true
                ? value as string ?? (value is byte[] bytes ? System.Text.Encoding.UTF8.GetString(bytes) : value?.ToString())
                : null;
    }

    private readonly ConcurrentDictionary<string, InMemoryExchange> exchanges = new();
    private readonly ConcurrentDictionary<string, InMemoryQueue> queues = new();

    internal ConcurrentDictionary<string, InMemoryExchange> Exchanges => exchanges;
    internal ConcurrentDictionary<string, InMemoryQueue> Queues => queues;

    /// <summary>Messages currently sitting in <paramref name="queue" /></summary>
    public int MessageCount(string queue) => queues.TryGetValue(queue, out var q) ? q.Count : 0;

    internal void DeclareExchange(ExchangeDefinition exchange)
        => exchanges.TryAdd(exchange.Name, new InMemoryExchange(exchange.Type));

    internal bool ExchangeExists(string exchange) => exchanges.ContainsKey(exchange);

    internal void DeleteExchange(string exchange) => exchanges.TryRemove(exchange, out _);

    internal string DeclareQueue(QueueDefinition queue)
    {
        var name = queue.Name.Length == 0 ? $"inmemory.gen-{Guid.NewGuid():N}" : queue.Name;
        queues.TryAdd(name, new InMemoryQueue(name, queue.Arguments));
        return name;
    }

    internal bool QueueExists(string queue) => queues.ContainsKey(queue);

    internal void DeleteQueue(string queue)
    {
        // like a broker cancelling the queue's consumers
        if (queues.TryRemove(queue, out var removed))
            removed.Deliveries.Writer.TryComplete();
    }

    internal void Purge(string queue)
    {
        if (!queues.TryGetValue(queue, out var q)) return;
        while (q.Deliveries.Reader.TryRead(out var delivery))
        {
            if (!delivery.TryTake()) Interlocked.Decrement(ref q.Expired);
        }
    }

    internal void Bind(BindingDefinition binding)
    {
        var exchange = exchanges.GetOrAdd(binding.Source, static _ => new InMemoryExchange("topic"));
        // AMQP bindings are idempotent: binding the same key twice must not deliver twice
        lock (exchange)
        {
            if (!exchange.Bindings.Contains(binding))
                exchange.Bindings = [.. exchange.Bindings, binding];
        }
    }

    internal void Unbind(BindingDefinition binding)
    {
        if (!exchanges.TryGetValue(binding.Source, out var exchange)) return;
        lock (exchange)
            exchange.Bindings = exchange.Bindings.Where(b => b != binding).ToArray();
    }

    internal InMemoryQueue? GetQueue(string queue) => queues.TryGetValue(queue, out var q) ? q : null;

    /// <summary>Routes one message; the body is copied because publishers reuse their buffers</summary>
    /// <returns>Whether at least one queue received the message (an AMQP mandatory publish is returned otherwise)</returns>
    internal bool Publish(string exchangeName, string routingKey, in MessageProperties properties, ReadOnlyMemory<byte> body)
    {
        var delivery = new InMemoryDelivery(exchangeName, routingKey, properties, body.ToArray());

        if (exchangeName.Length == 0)
        {
            // default exchange: routing key = queue name
            return GetQueue(routingKey) is { } queue && Enqueue(queue, delivery);
        }

        return Route(exchangeName, routingKey, delivery, depth: 0);
    }

    // a requeued message is not expired again
    internal void Redeliver(string queue, InMemoryDelivery delivery)
        => GetQueue(queue)?.Deliveries.Writer.TryWrite(delivery with { Redelivered = true, Expiry = null });

    private bool Enqueue(InMemoryQueue queue, InMemoryDelivery delivery)
    {
        var ttl = Min(queue.MessageTtl, delivery.Properties.Expiration);
        if (ttl is null)
            return queue.Deliveries.Writer.TryWrite(delivery);

        // one copy per queue: each queue expires its own
        var timed = delivery with { Expiry = new StrongBox<int>(0) };
        if (!queue.Deliveries.Writer.TryWrite(timed)) return false;
        _ = Task.Delay(ttl.Value).ContinueWith(_ => Expire(queue, timed), CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
        return true;
    }

    private void Expire(InMemoryQueue queue, InMemoryDelivery delivery)
    {
        if (Interlocked.CompareExchange(ref delivery.Expiry!.Value, 2, 0) != 0) return;
        Interlocked.Increment(ref queue.Expired);
        if (queue.DeadLetterExchange is not { } deadLetterExchange || !queues.ContainsKey(queue.Name)) return;

        // as RabbitMQ does: the per-message expiration is dropped so the message does not expire again
        var deadLetter = delivery with { Properties = delivery.Properties with { Expiration = null }, Expiry = null, Redelivered = false };
        var routingKey = queue.DeadLetterRoutingKey ?? delivery.RoutingKey;
        if (deadLetterExchange.Length == 0)
        {
            if (GetQueue(routingKey) is { } target) Enqueue(target, deadLetter with { Exchange = "", RoutingKey = routingKey });
        }
        else
        {
            Route(deadLetterExchange, routingKey, deadLetter with { Exchange = deadLetterExchange, RoutingKey = routingKey }, depth: 0);
        }
    }

    private static TimeSpan? Min(TimeSpan? a, TimeSpan? b) => a is null ? b : b is null ? a : a < b ? a : b;

    private bool Route(string exchangeName, string routingKey, InMemoryDelivery delivery, int depth)
    {
        if (depth > 8 || !exchanges.TryGetValue(exchangeName, out var exchange)) return false;
        var routed = false;

        foreach (var binding in exchange.Bindings)
        {
            var matches = exchange.Type switch
            {
                "fanout" => true,
                "direct" => binding.RoutingKey == routingKey,
                _ => TopicMatcher.Matches(binding.RoutingKey, routingKey)
            };
            if (!matches) continue;

            if (binding.DestinationIsExchange)
                routed |= Route(binding.Destination, routingKey, delivery, depth + 1);
            else
                routed |= GetQueue(binding.Destination) is { } queue && Enqueue(queue, delivery);
        }

        return routed;
    }
}

/// <summary>
///     AMQP topic matching: '.'-separated words, '*' matches one word, '#' matches zero or more
/// </summary>
internal static class TopicMatcher
{
    public static bool Matches(string pattern, string routingKey)
    {
        if (pattern == "#") return true;
        return Matches(pattern.Split('.'), 0, routingKey.Split('.'), 0);
    }

    private static bool Matches(string[] pattern, int p, string[] key, int k)
    {
        while (true)
        {
            if (p == pattern.Length) return k == key.Length;
            if (pattern[p] == "#")
            {
                if (p == pattern.Length - 1) return true;
                for (var skip = k; skip <= key.Length; skip++)
                    if (Matches(pattern, p + 1, key, skip))
                        return true;
                return false;
            }

            if (k == key.Length) return false;
            if (pattern[p] != "*" && pattern[p] != key[k]) return false;
            p++;
            k++;
        }
    }
}
