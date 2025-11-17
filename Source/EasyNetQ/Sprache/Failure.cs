namespace EasyNetQ.Sprache;

internal sealed class Failure<T> : IFailure<T>
{
    private readonly Func<IEnumerable<string>> expectations;
    private readonly Func<string> message;

    public Failure(Input input, Func<string> message, Func<IEnumerable<string>> expectations)
    {
        FailedInput = input;
        this.message = message;
        this.expectations = expectations;
    }

    public string Message => message();

    public IEnumerable<string> Expectations => expectations();

    public Input FailedInput { get; }

    public override string ToString()
    {
        var expMsg = "";

        if (Expectations.Any())
            expMsg = " expected " + Expectations.Aggregate((e1, e2) => e1 + " or " + e2);

        return $"Parsing failure: {Message};{expMsg} ({FailedInput}).";
    }
}
