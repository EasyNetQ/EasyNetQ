namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class QueueAttribute : Attribute
{
    public QueueAttribute()
    {

    }
    public QueueAttribute(string name, string? type = null)
    {
        Name = name;
        Type = type;
    }

    public string? Name { get; init; }

    public string? Type { get; init; }

}
