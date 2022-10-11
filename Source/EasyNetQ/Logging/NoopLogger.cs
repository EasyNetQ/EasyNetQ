using System;

namespace EasyNetQ.Logging;

/// <summary>
/// Logger that does nothing. It is used by default.
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
