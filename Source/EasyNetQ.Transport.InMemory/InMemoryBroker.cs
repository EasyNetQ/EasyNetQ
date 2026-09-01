using System.Collections.Concurrent;
using System.Threading.Channels;

namespace EasyNetQ.Transport.InMemory;

/// <summary>
///     One message routed to a queue
/// </summary>
public sealed record InMemoryDelivery(string Exchange, string RoutingKey, MessageProperties Properties, byte[] Body, bool Redelivered = false);

/// <summary>
///     The in-process broker: exchanges, queues, bindings, AMQP-style topic matching. The default exchange
///     ("") routes the routing key straight to the queue of that name.
/// </summary>
public sealed class InMemoryBroker
{
    internal sealed class InMemoryExchange(string type)
    {
        public string Type { get; } = type;
        public ConcurrentBag<BindingDefinition> Bindings { get; } = new();
    }

    internal sealed class InMemoryQueue
    {
        public Channel<InMemoryDelivery> Deliveries { get; } = Channel.CreateUnbounded<InMemoryDelivery>();
        public int ConsumerCount;
    }

    private readonly ConcurrentDictionary<string, InMemoryExchange> exchanges = new();
    private readonly ConcurrentDictionary<string, InMemoryQueue> queues = new();

    internal ConcurrentDictionary<string, InMemoryExchange> Exchanges => exchanges;
    internal ConcurrentDictionary<string, InMemoryQueue> Queues => queues;

    /// <summary>Messages currently sitting in <paramref name="queue" /></summary>
    public int MessageCount(string queue) => queues.TryGetValue(queue, out var q) ? q.Deliveries.Reader.Count : 0;

    internal void DeclareExchange(ExchangeDefinition exchange)
        => exchanges.TryAdd(exchange.Name, new InMemoryExchange(exchange.Type));

    internal bool ExchangeExists(string exchange) => exchanges.ContainsKey(exchange);

    internal void DeleteExchange(string exchange) => exchanges.TryRemove(exchange, out _);

    internal string DeclareQueue(QueueDefinition queue)
    {
        var name = queue.Name.Length == 0 ? $"inmemory.gen-{Guid.NewGuid():N}" : queue.Name;
        queues.TryAdd(name, new InMemoryQueue());
        return name;
    }

    internal bool QueueExists(string queue) => queues.ContainsKey(queue);

    internal void DeleteQueue(string queue) => queues.TryRemove(queue, out _);

    internal void Purge(string queue)
    {
        if (!queues.TryGetValue(queue, out var q)) return;
        while (q.Deliveries.Reader.TryRead(out _)) { }
    }

    internal void Bind(BindingDefinition binding)
    {
        var exchange = exchanges.GetOrAdd(binding.Source, static _ => new InMemoryExchange("topic"));
        exchange.Bindings.Add(binding);
    }

    internal void Unbind(BindingDefinition binding)
    {
        if (!exchanges.TryGetValue(binding.Source, out var exchange)) return;
        // ConcurrentBag has no remove; rebuild without the binding
        var remaining = exchange.Bindings.Where(b => b != binding).ToList();
        while (exchange.Bindings.TryTake(out _))
        {
        }
        foreach (var b in remaining) exchange.Bindings.Add(b);
    }

    internal InMemoryQueue? GetQueue(string queue) => queues.TryGetValue(queue, out var q) ? q : null;

    /// <summary>Routes one message; the body is copied because publishers reuse their buffers</summary>
    internal void Publish(string exchangeName, string routingKey, in MessageProperties properties, ReadOnlyMemory<byte> body)
    {
        var delivery = new InMemoryDelivery(exchangeName, routingKey, properties, body.ToArray());

        if (exchangeName.Length == 0)
        {
            // default exchange: routing key = queue name
            GetQueue(routingKey)?.Deliveries.Writer.TryWrite(delivery);
            return;
        }

        Route(exchangeName, routingKey, delivery, depth: 0);
    }

    internal void Redeliver(string queue, InMemoryDelivery delivery)
        => GetQueue(queue)?.Deliveries.Writer.TryWrite(delivery with { Redelivered = true });

    private void Route(string exchangeName, string routingKey, InMemoryDelivery delivery, int depth)
    {
        if (depth > 8 || !exchanges.TryGetValue(exchangeName, out var exchange)) return;

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
                Route(binding.Destination, routingKey, delivery, depth + 1);
            else
                GetQueue(binding.Destination)?.Deliveries.Writer.TryWrite(delivery);
        }
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
