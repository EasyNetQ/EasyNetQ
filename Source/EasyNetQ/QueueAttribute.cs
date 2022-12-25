namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class QueueAttribute : Attribute
{
    internal static readonly QueueAttribute Default = new();

    public string? Name { get; set; }

    public string? Type { get; set; }

}
