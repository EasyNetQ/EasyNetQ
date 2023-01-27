using System.Collections.Concurrent;
using System.Diagnostics;
using EasyNetQ.IntegrationTests.Utils;
using EasyNetQ.Internals;
using EasyNetQ.Topology;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_tracecontext_propagation : IDisposable
{
    private readonly SelfHostedBus bus;

    public When_publish_and_subscribe_with_tracecontext_propagation(RabbitMQFixture rmqFixture)
    {
        bus = RabbitHutch.CreateBus($"host={rmqFixture.Host};prefetchCount=1;timeout=-1;publisherConfirms=True");
    }

    public void Dispose() => bus.Dispose();

    [Fact]
    public async Task Test()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var cancellationToken = cts.Token;
        using var activitySource = new ActivitySource("Tests");
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => source.Name == "EasyNetQ" || source.Name == "Tests",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        var activityDict = new ConcurrentDictionary<Activity, bool>();
        var messagesSink = new MessagesSink(1);
        var traceparent = "";
        try
        {
            listener.ActivityStarted += ActivityStarted;
            ActivitySource.AddActivityListener(listener);

            var subscriptionId = Guid.NewGuid().ToString();

            using var subscription = await bus.PubSub.SubscribeAsync<Message>(
                subscriptionId,
                messagesSink.Receive,
                cfg => cfg.WithTraceContext(),
                cancellationToken);

            using (var senderActivity = activitySource.StartActivity("Parent"))
            {
                traceparent = senderActivity?.Id;
                await bus.PubSub.PublishAsync(new Message(1), cfg => cfg.WithTraceContext(), cancellationToken);
            }

            await messagesSink.WaitAllReceivedAsync(cancellationToken);
        }
        finally
        {
            listener.ActivityStarted -= ActivityStarted;
            listener.Dispose();
        }
        var activities = activityDict.Keys.OrderBy(x => x.StartTimeUtc).ToArray();
        activities.Should().HaveCountGreaterThanOrEqualTo(3);
        var (parent, publish, consume) = (activities[0], activities[1], activities[2]);
        // bus should create pubsub spans
        parent.DisplayName.Should().Be("Parent");
        publish.DisplayName.Should().Be("Publish");
        consume.DisplayName.Should().Be("Consume");
        activities.Count(x => x.DisplayName == "Parent").Should().Be(1);
        activities.Count(x => x.DisplayName == "Publish").Should().Be(1);
        activities.Count(x => x.DisplayName == "Consume").Should().Be(1);
        // traceId should propagate across
        parent.TraceId.Should().Be(publish.TraceId);
        parent.TraceId.Should().Be(consume.TraceId);
        parent.Id.Should().Be(publish.ParentId);
        publish.Id.Should().Be(consume.ParentId);

        traceparent.Should().Contain(consume.TraceId.ToHexString());

        void ActivityStarted(Activity activity) => activityDict.TryAdd(activity, default).Should().BeTrue();
    }
}
