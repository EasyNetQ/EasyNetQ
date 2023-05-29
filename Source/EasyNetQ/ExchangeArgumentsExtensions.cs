namespace EasyNetQ;

public readonly struct ExchangeArgumentsBuilder
{
    public static ExchangeArgumentsBuilder Empty => default;

    public static ExchangeArgumentsBuilder From(IDictionary<string, object> arguments) => new(arguments);


    private readonly IDictionary<string, object>? arguments;

    private ExchangeArgumentsBuilder(IDictionary<string, object> arguments) => this.arguments = arguments;

    public ExchangeArgumentsBuilder WithArgument(string name, object value)
    {
        var newArguments = arguments ?? new Dictionary<string, object>();
        newArguments[name] = value;
        return new ExchangeArgumentsBuilder(newArguments);
    }

    public IDictionary<string, object>? Build() => arguments;
}

public static class ExchangeArgumentsExtensions
{
    public static ExchangeArgumentsBuilder WithAlternateExchange(this ExchangeArgumentsBuilder arguments, string alternateExchange) =>
        arguments.WithArgument(ExchangeArgument.AlternateExchange, alternateExchange);

    public static ExchangeArgumentsBuilder WithDelayedExchangeType(this ExchangeArgumentsBuilder arguments, string delayedType) =>
        arguments.WithArgument(ExchangeArgument.DelayedType, delayedType);
}
