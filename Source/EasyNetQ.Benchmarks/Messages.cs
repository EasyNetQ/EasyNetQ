using EasyNetQ;

namespace EasyNetQ.Benchmarks;

public class SmallMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class MediumMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = [];
}

public class LargeMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = [];
    public Dictionary<string, string> Metadata { get; set; } = [];
    public List<LargeMessageItem> Items { get; set; } = [];
}

public class LargeMessageItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

[Exchange(Name = "custom-exchange")]
[Queue(Name = "custom-queue")]
public class AttributedMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public static class SampleMessages
{
    public static SmallMessage CreateSmall() => new()
    {
        Id = 1,
        Name = "Test"
    };

    public static MediumMessage CreateMedium() => new()
    {
        Id = 42,
        Name = "John Doe",
        Email = "john@example.com",
        Description = "A medium-sized message with several properties for benchmarking purposes.",
        CreatedAt = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        Tags = ["benchmark", "test", "performance", "easynetq"],
        Metadata = new Dictionary<string, string>
        {
            ["source"] = "benchmark",
            ["version"] = "1.0",
            ["environment"] = "production"
        }
    };

    public static LargeMessage CreateLarge() => new()
    {
        Id = 99,
        Name = "Jane Smith",
        Email = "jane@example.com",
        Description = "A large message containing a collection of items for benchmarking serialization performance with realistic payloads.",
        CreatedAt = new DateTime(2024, 6, 1, 14, 0, 0, DateTimeKind.Utc),
        Tags = ["benchmark", "test", "performance", "easynetq", "large", "payload", "serialization"],
        Metadata = new Dictionary<string, string>
        {
            ["source"] = "benchmark",
            ["version"] = "2.0",
            ["environment"] = "production",
            ["region"] = "us-east-1",
            ["priority"] = "high"
        },
        Items = Enumerable.Range(1, 50).Select(i => new LargeMessageItem
        {
            Id = i,
            Title = $"Item {i} - Product with a moderately long description for realism",
            Price = 9.99m + i,
            Quantity = i * 2
        }).ToList()
    };

    public static object Create(string size) => size switch
    {
        "Small" => CreateSmall(),
        "Medium" => CreateMedium(),
        "Large" => CreateLarge(),
        _ => throw new ArgumentException($"Unknown size: {size}", nameof(size))
    };

    public static Type GetType(string size) => size switch
    {
        "Small" => typeof(SmallMessage),
        "Medium" => typeof(MediumMessage),
        "Large" => typeof(LargeMessage),
        _ => throw new ArgumentException($"Unknown size: {size}", nameof(size))
    };
}
