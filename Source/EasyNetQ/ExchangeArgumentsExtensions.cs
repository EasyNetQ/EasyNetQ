namespace EasyNetQ;

using Arguments = IDictionary<string, object>;

public static class ExchangeArgumentsExtensions
{
    public static Arguments WithExchangeAlternate(this Arguments arguments, string alternateExchange) =>
        arguments.WithExchangeArgument(ExchangeArgument.AlternateExchange, alternateExchange);

    public static Arguments WithExchangeDelayedType(this Arguments arguments, string delayedType) =>
        arguments.WithExchangeArgument(ExchangeArgument.DelayedType, delayedType);

    public static IDictionary<string, object> WithExchangeArgument(this Arguments arguments, string key, object value)
    {
        arguments[key] = value;
        return arguments;
    }
}
