using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EasyNetQ.Diagnostics;

/// <summary>
///     The single <see cref="ActivitySource" /> and <see cref="Meter" /> for EasyNetQ telemetry. Enable with
///     <c>AddSource("EasyNetQ")</c> / <c>AddMeter("EasyNetQ")</c>; the RabbitMQ wire spans stay with the client's
///     own sources (<c>RabbitMQ.Client.Publisher</c>/<c>RabbitMQ.Client.Subscriber</c>) - EasyNetQ only emits the
///     semantic layer on top (message processing, publish operations, RPC, error queue).
/// </summary>
public static class EasyNetQDiagnostics
{
    /// <summary>
    ///     The name of both the activity source and the meter
    /// </summary>
    public const string SourceName = "EasyNetQ";

    private static readonly string Version =
        typeof(EasyNetQDiagnostics).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    /// <summary>
    ///     The activity source for EasyNetQ spans
    /// </summary>
    public static readonly ActivitySource Source = new(SourceName, Version);

    /// <summary>
    ///     The meter for EasyNetQ instruments
    /// </summary>
    public static readonly Meter Meter = new(SourceName, Version);

#if NET9_0_OR_GREATER
    private static readonly InstrumentAdvice<double> DurationAdvice = new()
    {
        // semconv advised buckets for messaging durations
        HistogramBucketBoundaries = [0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
    };
#endif

    /// <summary>Number of messages producer attempted to send to the broker</summary>
    public static readonly Counter<long> SentMessages =
        Meter.CreateCounter<long>("messaging.client.sent.messages", "{message}", "Number of messages producer attempted to send to the broker");

    /// <summary>Number of messages delivered to the application's handlers</summary>
    public static readonly Counter<long> ConsumedMessages =
        Meter.CreateCounter<long>("messaging.client.consumed.messages", "{message}", "Number of messages delivered to the application");

    /// <summary>Duration of publish operations, in seconds</summary>
    public static readonly Histogram<double> OperationDuration =
#if NET9_0_OR_GREATER
        Meter.CreateHistogram<double>("messaging.client.operation.duration", "s", "Duration of messaging operations initiated by a producer or consumer client", advice: DurationAdvice);
#else
        Meter.CreateHistogram<double>("messaging.client.operation.duration", "s", "Duration of messaging operations initiated by a producer or consumer client");
#endif

    /// <summary>Duration of processing one delivered message, in seconds</summary>
    public static readonly Histogram<double> ProcessDuration =
#if NET9_0_OR_GREATER
        Meter.CreateHistogram<double>("messaging.process.duration", "s", "Duration of processing operation", advice: DurationAdvice);
#else
        Meter.CreateHistogram<double>("messaging.process.duration", "s", "Duration of processing operation");
#endif

    /// <summary>Number of processed messages by acknowledgement decision (easynetq.ack.decision)</summary>
    public static readonly Counter<long> ConsumerMessages =
        Meter.CreateCounter<long>("easynetq.consumer.messages", "{message}", "Number of processed messages by acknowledgement decision");

    /// <summary>Messages currently being processed by consumers</summary>
    public static readonly UpDownCounter<long> ConsumerInFlight =
        Meter.CreateUpDownCounter<long>("easynetq.consumer.in_flight", "{message}", "Messages currently being processed by consumers");
}
