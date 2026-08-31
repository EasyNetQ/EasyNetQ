namespace EasyNetQ;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class ExchangeAttribute : Attribute
{
    internal static readonly ExchangeAttribute Default = new(null);
    public ExchangeAttribute()
    {

    }
    public ExchangeAttribute(string name, string exchangeType = null)
    {
        Name = name;
        ExchangeType = exchangeType ?? EasyNetQ.ExchangeType.Topic;
    }
    public string Name { get; init; }
    public string ExchangeType { get; init; }
}
