using System.Diagnostics;
using System.Diagnostics.Metrics;
using EasyNetQ.Diagnostics;

namespace EasyNetQ.AspNetCore.SignalR.Internal;

/// <summary>Backplane instruments on the shared <c>EasyNetQ</c> meter</summary>
internal static class BackplaneMetrics
{
    private static readonly Counter<long> Messages = EasyNetQDiagnostics.Meter.CreateCounter<long>(
        "easynetq.signalr.messages", "{message}", "Backplane messages published and received, by kind"
    );

    private static readonly UpDownCounter<long> Connections = EasyNetQDiagnostics.Meter.CreateUpDownCounter<long>(
        "easynetq.signalr.connections", "{connection}", "SignalR connections held by this server"
    );

    private static readonly Counter<long> AckTimeouts = EasyNetQDiagnostics.Meter.CreateCounter<long>(
        "easynetq.signalr.ack_timeouts", "{ack}", "Cross-server group changes no server acknowledged in time"
    );

    private static readonly Counter<long> Resyncs = EasyNetQDiagnostics.Meter.CreateCounter<long>(
        "easynetq.signalr.resyncs", "{resync}", "Queue and binding redeclarations after a connection recovery"
    );

    public static void Published(string hub, BackplaneMessageKind kind) => Record(hub, kind, "publish");

    public static void Received(string hub, BackplaneMessageKind kind) => Record(hub, kind, "receive");

    public static void ConnectionAdded(string hub) => Connections.Add(1, new KeyValuePair<string, object?>("signalr.hub", hub));

    public static void ConnectionRemoved(string hub) => Connections.Add(-1, new KeyValuePair<string, object?>("signalr.hub", hub));

    public static void AckTimedOut(string hub) => AckTimeouts.Add(1, new KeyValuePair<string, object?>("signalr.hub", hub));

    public static void Resynced(string hub) => Resyncs.Add(1, new KeyValuePair<string, object?>("signalr.hub", hub));

    private static void Record(string hub, BackplaneMessageKind kind, string direction)
    {
        if (!Messages.Enabled) return;
        var tags = new TagList
        {
            { "signalr.hub", hub },
            { "easynetq.signalr.kind", KindName(kind) },
            { "easynetq.signalr.direction", direction },
        };
        Messages.Add(1, tags);
    }

    private static string KindName(BackplaneMessageKind kind) => kind switch
    {
        BackplaneMessageKind.Invocation => "invocation",
        BackplaneMessageKind.GroupCommand => "group",
        BackplaneMessageKind.Ack => "ack",
        BackplaneMessageKind.Completion => "completion",
        _ => "unknown",
    };
}
