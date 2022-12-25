namespace EasyNetQ.Logging;

/// <summary>
/// Logger that does nothing. It is used/registered by default in the
/// EasyNetQ components if custom logger has not yet been registered.
/// </summary>
public sealed class NoopLogger<TCategoryName> : ILogger<TCategoryName>
{
    /// <inheritdoc />
    public bool Log(
        LogLevel logLevel,
        Func<string>? messageFunc,
        Exception? exception = null,
        params object?[] formatParameters
    ) => false;
}
