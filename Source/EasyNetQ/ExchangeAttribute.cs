namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ExchangeAttribute : Attribute
{
    internal static readonly ExchangeAttribute Default = new();

    public string? Name { get; set; }
}
