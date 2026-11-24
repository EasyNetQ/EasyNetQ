using Microsoft.Extensions.Logging;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

// LoggerMessage.Define instead of the source generator: no generator dependency, still allocation-free and AOT-safe
internal static class BackplaneLog
{
    private static readonly Action<ILogger, string, string, Exception?> Started = LoggerMessage.Define<string, string>(
        LogLevel.Information, new EventId(1, "BackplaneStarted"), "SignalR backplane for {Hub} started on queue {Queue}"
    );

    private static readonly Action<ILogger, string, Exception?> Resynced = LoggerMessage.Define<string>(
        LogLevel.Information, new EventId(2, "BackplaneResynced"), "SignalR backplane for {Hub} redeclared its queue and bindings after a recovery"
    );

    private static readonly Action<ILogger, string, Exception?> ResyncFailed = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(3, "BackplaneResyncFailed"), "SignalR backplane for {Hub} could not redeclare its queue and bindings; retrying on the next recovery"
    );

    private static readonly Action<ILogger, string, Exception?> InvalidMessage = LoggerMessage.Define<string>(
        LogLevel.Warning, new EventId(4, "InvalidBackplaneMessage"), "SignalR backplane for {Hub} dropped a message it could not process"
    );

    private static readonly Action<ILogger, string, string, Exception?> UnknownCompletion = LoggerMessage.Define<string, string>(
        LogLevel.Debug, new EventId(5, "UnknownCompletion"), "SignalR backplane for {Hub} received a result for unknown invocation {InvocationId}"
    );

    private static readonly Action<ILogger, string, string, Exception?> UnknownProtocol = LoggerMessage.Define<string, string>(
        LogLevel.Warning, new EventId(6, "UnknownProtocol"), "SignalR backplane for {Hub} received a client result in protocol {Protocol}, which this server does not support"
    );

    private static readonly Action<ILogger, string, Exception?> WriteFailed = LoggerMessage.Define<string>(
        LogLevel.Debug, new EventId(7, "WriteFailed"), "SignalR backplane for {Hub} failed to write to a local connection"
    );

    public static void BackplaneStarted(this ILogger logger, string hub, string queue) => Started(logger, hub, queue, null);
    public static void BackplaneResynced(this ILogger logger, string hub) => Resynced(logger, hub, null);
    public static void BackplaneResyncFailed(this ILogger logger, string hub, Exception exception) => ResyncFailed(logger, hub, exception);
    public static void InvalidBackplaneMessage(this ILogger logger, string hub, Exception exception) => InvalidMessage(logger, hub, exception);
    public static void UnknownCompletionReceived(this ILogger logger, string hub, string invocationId) => UnknownCompletion(logger, hub, invocationId, null);
    public static void UnknownProtocolReceived(this ILogger logger, string hub, string protocol) => UnknownProtocol(logger, hub, protocol, null);
    public static void LocalWriteFailed(this ILogger logger, string hub, Exception exception) => WriteFailed(logger, hub, exception);
}
