using EasyNetQ.MessageVersioning;

namespace EasyNetQ.Examples.Soak;

public abstract class Shape
{
    public string Label { get; set; } = "";
}

public sealed class Circle : Shape
{
    public double Radius { get; set; }
}

public sealed class Square : Shape
{
    public double Side { get; set; }
}

public sealed class Drawing
{
    public string Run { get; set; } = "";
    public int Id { get; set; }
    public Shape? Main { get; set; }
    public List<Shape> Shapes { get; set; } = new();
    public Dictionary<string, int> Tags { get; set; } = new();
    public decimal Amount { get; set; }
    public DateTimeOffset At { get; set; }
}

public sealed record Payload(string Run, int Id, string Text);

public sealed record CompressedPayload(string Run, int Id, string Text);

public class OrderV1
{
    public string Run { get; set; } = "";
    public int Id { get; set; }
    public string Customer { get; set; } = "";
}

public class OrderV2 : OrderV1, ISupersede<OrderV1>
{
    public decimal Total { get; set; }
}

public interface IAnimal
{
    string Run { get; }
    int Id { get; }
    string Sound { get; }
}

public sealed class Dog : IAnimal
{
    public string Run { get; set; } = "";
    public int Id { get; set; }
    public string Sound { get; set; } = "woof";
}

public sealed class Cat : IAnimal
{
    public string Run { get; set; } = "";
    public int Id { get; set; }
    public string Sound { get; set; } = "meow";
}

public sealed record Prioritized(string Run, int Id, byte Priority);

public sealed record FluentPrioritized(string Run, int Id, byte Priority);

public sealed record Tick(string Run, int Producer, int Id);

public sealed record Confirmed(string Run, int Id);

public sealed record Unroutable(string Run, int Id);

public sealed record RpcRequest(string Run, int Value, int DelayMilliseconds, bool Fail);

public sealed record RpcResponse(string Run, int Value);

public sealed record CommandA(string Run, int Id);

public sealed record CommandB(string Run, int Id);

public sealed record TopicEvent(string Run, int Id);

public sealed record SyncEvent(string Run, int Id);

public sealed record AsyncEvent(string Run, int Id);

public sealed record Scheduled(string Run, int Id, DateTime PublishedAt);

public sealed record DelayedScheduled(string Run, int Id, DateTime PublishedAt);

public sealed record Ordered(string Run, int Id);

public sealed record Poison(string Run, int Id, bool Fail);

public sealed record Expiring(string Run, int Id, string Kind);

public sealed record Streamed(string Run, int Id);

public sealed record Quorumed(string Run, int Id);

public sealed record Doomed(string Run, int Id);
