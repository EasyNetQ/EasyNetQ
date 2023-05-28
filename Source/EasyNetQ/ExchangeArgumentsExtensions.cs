namespace EasyNetQ;

public static class ExchangeArgumentsExtensions
{
    public static IDictionary<string, object> WithExchangeAlternate(this IDictionary<string, object>? arguments, string alternateExchange) =>
        arguments.WithExchangeArgument(ExchangeArgument.AlternateExchange, alternateExchange);

    public static IDictionary<string, object> WithExchangeDelayedType(this IDictionary<string, object>? arguments, string delayedType) =>
        arguments.WithExchangeArgument(ExchangeArgument.DelayedType, delayedType);

    public static IDictionary<string, object> WithExchangeArgument(this IDictionary<string, object>? arguments, string key, object value)
    {
        (arguments ??= new Dictionary<string, object>()).Add(key, value);
        return arguments;
    }
}
