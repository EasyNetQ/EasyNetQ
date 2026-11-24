namespace EasyNetQ;

/// <summary>
///     A mandatory publish matched no queue and the broker returned it. Transport-agnostic, so code above the
///     transport layer can tell "nobody is listening" from other publish failures.
/// </summary>
public class UnroutableMessageException : Exception
{
    /// <inheritdoc />
    public UnroutableMessageException()
    {
    }

    /// <inheritdoc />
    public UnroutableMessageException(string message) : base(message)
    {
    }

    /// <inheritdoc />
    public UnroutableMessageException(string message, Exception inner) : base(message, inner)
    {
    }
}
