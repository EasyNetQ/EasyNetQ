namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ExchangeAttribute : Attribute
{
    internal static readonly ExchangeAttribute Default = new(null);
    public ExchangeAttribute()
    {

    }
    public ExchangeAttribute(string name)
    {
        Name = name;
    }
    public string Name { get; init; }
}
