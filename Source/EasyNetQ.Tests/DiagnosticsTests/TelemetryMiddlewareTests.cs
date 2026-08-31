using System.Diagnostics;
using System.Diagnostics.Metrics;
using EasyNetQ.Diagnostics;
using EasyNetQ.Events;
using EasyNetQ.Tests.Mocking;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.Tests.DiagnosticsTests;

// The ActivityListener/MeterListener these tests register are process-global: while they are active every
// publish in the process injects trace headers, which breaks concurrently running tests that assert exact
// properties. Run this collection with parallelization disabled.
[CollectionDefinition("TelemetryListeners", DisableParallelization = true)]
public sealed class TelemetryListenersCollection;

[Collection("TelemetryListeners")]
public class TelemetryMiddlewareTests
{
    private static ActivityListener CreateListener(ICollection<Activity> activities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == EasyNetQDiagnostics.SourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activities.Add
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task Should_emit_publish_span_and_inject_traceparent()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        await using var mockBuilder = new MockBuilder();

        await mockBuilder.Bus.Advanced.PublishAsync(
            "telemetry_exchange", "telemetry_key", false, false,
            new MessageProperties { CorrelationId = "corr-1" }, new byte[] { 1, 2, 3 }, CancellationToken.None
        );

        var activity = activities.Should().ContainSingle(a => a.OperationName == "publish telemetry_exchange").Subject;
        activity.Kind.Should().Be(ActivityKind.Producer);
        activity.GetTagItem(MessagingTags.DestinationName).Should().Be("telemetry_exchange");
        activity.GetTagItem(MessagingTags.OperationName).Should().Be("publish");
        activity.GetTagItem(MessagingTags.ConversationId).Should().Be("corr-1");

        var publishCall = mockBuilder.Channels[0].ReceivedCalls().Single(c => c.GetMethodInfo().Name == "BasicPublishAsync");
        var properties = (RabbitMQ.Client.BasicProperties)publishCall.GetArguments()[3]!;
        properties.Headers.Should().ContainKey("traceparent");
        ((string)properties.Headers!["traceparent"]!).Should().Contain(activity.TraceId.ToHexString());
    }

    [Fact]
    public async Task Should_emit_process_span_parented_from_traceparent_header()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);
        await using var mockBuilder = new MockBuilder();

        await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Topology.Queue("telemetry_queue", false),
            (_, _, _) => Task.FromResult(AckDecision.Ack),
            c => c.WithConsumerTag("telemetry_consumer")
        );

        const string traceParent = "00-0123456789abcdef0123456789abcdef-0123456789abcdef-01";
        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "telemetry_consumer", 1, false, "telemetry_exchange", "telemetry_key",
            new RabbitMQ.Client.BasicProperties
            {
                CorrelationId = "corr-2",
                Headers = new Dictionary<string, object?> { ["traceparent"] = traceParent }
            },
            "{}"u8.ToArray(),
            TestContext.Current.CancellationToken
        );

        var activity = activities.Should().ContainSingle(a => a.OperationName == "process telemetry_queue").Subject;
        activity.Kind.Should().Be(ActivityKind.Consumer);
        activity.TraceId.ToHexString().Should().Be("0123456789abcdef0123456789abcdef");
        activity.GetTagItem(MessagingTags.DestinationSubscriptionName).Should().Be("telemetry_queue");
        activity.GetTagItem(MessagingTags.AckDecision).Should().Be("ack");
        activity.Status.Should().NotBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task Should_emit_rpc_client_span_around_request_response()
    {
        var activities = new List<Activity>();
        using var listener = CreateListener(activities);

        var correlationId = Guid.NewGuid().ToString();
        await using var mockBuilder = new MockBuilder(
            c => c.AddSingleton<ICorrelationIdGenerationStrategy>(_ => new StaticCorrelationIdGenerationStrategy(correlationId))
        );

        using var waiter = new CountdownEvent(2);
#pragma warning disable IDISP004
        mockBuilder.Published += () => waiter.Signal();
        mockBuilder.EventBus.Subscribe((StartConsumingSucceededEvent _) => Task.FromResult(waiter.Signal()));
#pragma warning restore IDISP004

        var task = mockBuilder.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(
            new TestRequestMessage(), TestContext.Current.CancellationToken
        );
        waiter.Wait(5000, TestContext.Current.CancellationToken).Should().BeTrue();

        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "consumer_tag", 0, false, "the_exchange", "the_routing_key",
            new RabbitMQ.Client.BasicProperties
            {
                Type = "EasyNetQ.Tests.TestResponseMessage, EasyNetQ.Tests",
                CorrelationId = correlationId
            },
            "{ Id:12, Text:\"Hello\"}"u8.ToArray(),
            TestContext.Current.CancellationToken
        );
        await task;

        var rpcActivity = activities.Should().ContainSingle(a => a.OperationName.StartsWith("rpc ")).Subject;
        rpcActivity.Kind.Should().Be(ActivityKind.Client);
        rpcActivity.GetTagItem(MessagingTags.ConversationId).Should().Be(correlationId);
        rpcActivity.Status.Should().NotBe(ActivityStatusCode.Error);
    }

    [Fact]
    public async Task Should_record_publish_and_consume_metrics()
    {
        var measurements = new List<(string Instrument, long Value, string? Destination, string? Queue, string? Ack)>();
        using var meterListener = new MeterListener();
        meterListener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Meter.Name == EasyNetQDiagnostics.SourceName && instrument is Counter<long> or UpDownCounter<long>)
                l.EnableMeasurementEvents(instrument);
        };
        meterListener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
        {
            string? destination = null, queue = null, ack = null;
            foreach (var tag in tags)
            {
                if (tag.Key == MessagingTags.DestinationName) destination = tag.Value as string;
                if (tag.Key == MessagingTags.DestinationSubscriptionName) queue = tag.Value as string;
                if (tag.Key == MessagingTags.AckDecision) ack = tag.Value as string;
            }
            lock (measurements) measurements.Add((instrument.Name, value, destination, queue, ack));
        });
        meterListener.Start();

        await using var mockBuilder = new MockBuilder();

        await mockBuilder.Bus.Advanced.PublishAsync(
            "metrics_exchange", "key", false, false, new MessageProperties(), new byte[] { 1 }, CancellationToken.None
        );
        await mockBuilder.Bus.Advanced.ConsumeAsync(
            new Topology.Queue("metrics_queue", false),
            (_, _, _) => Task.FromResult(AckDecision.Ack),
            c => c.WithConsumerTag("metrics_consumer")
        );
        await mockBuilder.Consumers[0].HandleBasicDeliverAsync(
            "metrics_consumer", 1, false, "metrics_exchange", "key",
            new RabbitMQ.Client.BasicProperties(), "{}"u8.ToArray(), TestContext.Current.CancellationToken
        );

        lock (measurements)
        {
            measurements.Should().Contain(m => m.Instrument == "messaging.client.sent.messages" && m.Value == 1 && m.Destination == "metrics_exchange");
            measurements.Should().Contain(m => m.Instrument == "messaging.client.consumed.messages" && m.Value == 1 && m.Queue == "metrics_queue");
            measurements.Should().Contain(m => m.Instrument == "easynetq.consumer.messages" && m.Ack == "ack" && m.Queue == "metrics_queue");
            measurements.Where(m => m.Instrument == "easynetq.consumer.in_flight").Sum(m => m.Value).Should().Be(0);
        }
    }
}
