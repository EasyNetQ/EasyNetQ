using System;

namespace EasyNetQ.Logging;

/// <summary>
/// Logger that does nothing. It is used by default.
/// </summary>
public class NoopLogger : ILogger
{
    /// <inheritdoc />
    public bool Log(
        LogLevel logLevel,
        Func<string> messageFunc,
        Exception exception = null,
        params object[] formatParameters
    )
    {
        return false;
    }
}

/// <summary>
/// Logger that does nothing. It is used by default.
/// </summary>
public class NoopLogger<TCategoryName> : ILogger<TCategoryName>
{
    /// <inheritdoc />
    public bool Log(
        LogLevel logLevel,
        Func<string> messageFunc,
        Exception exception = null,
        params object[] formatParameters
    )
    {
        return false;
    }
}
