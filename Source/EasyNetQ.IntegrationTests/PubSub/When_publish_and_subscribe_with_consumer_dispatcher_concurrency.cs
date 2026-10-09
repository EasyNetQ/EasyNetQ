using EasyNetQ.IntegrationTests.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace EasyNetQ.IntegrationTests.PubSub;

[Collection("RabbitMQ")]
public class When_publish_and_subscribe_with_consumer_dispatcher_concurrency : IDisposable, IAsyncLifetime
{
    private const int MessagesCount = 20;
    private const ushort ConnectionDispatcherConcurrency = 2;
    private const ushort ParallelDispatcherConcurrency = 6;
    private static readonly TimeSpan HandlingTime = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MaxWaitForExpectedConcurrency = TimeSpan.FromSeconds(1);

    private readonly ServiceProvider serviceProvider;
    private readonly IBus bus;

    public When_publish_and_subscribe_with_consumer_dispatcher_concurrency(RabbitMQFixture rmqFixture)
    {
        // One connection for all the subscriptions, with its own connection-wide dispatch concurrency
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddEasyNetQ($"host={rmqFixture.Host};consumerDispatcherConcurrency={ConnectionDispatcherConcurrency};timeout=-1");

        serviceProvider = serviceCollection.BuildServiceProvider();
        bus = serviceProvider.GetRequiredService<IBus>();
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public virtual void Dispose()
    {
        serviceProvider?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await serviceProvider.DisposeAsync();
    }

    [Fact]
    public async Task Should_dispatch_each_subscription_with_its_own_concurrency()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var messages = MessagesFactories.Create(MessagesCount);
        var sequential = new InFlightTracker(MessagesCount, 1);
        var parallel = new InFlightTracker(MessagesCount, ParallelDispatcherConcurrency);
        var inherited = new InFlightTracker(MessagesCount, ConnectionDispatcherConcurrency);
        var zero = new InFlightTracker(MessagesCount, ConnectionDispatcherConcurrency);

        await using (await bus.PubSub.SubscribeAsync<Message>(
            Guid.NewGuid().ToString(), sequential.HandleAsync, c => c.WithConsumerDispatcherConcurrency(1), cts.Token
        ))
        await using (await bus.PubSub.SubscribeAsync<Message>(
            Guid.NewGuid().ToString(), parallel.HandleAsync, c => c.WithConsumerDispatcherConcurrency(ParallelDispatcherConcurrency), cts.Token
        ))
        await using (await bus.PubSub.SubscribeAsync<Message>(
            Guid.NewGuid().ToString(), inherited.HandleAsync, _ => { }, cts.Token
        ))
        await using (await bus.PubSub.SubscribeAsync<Message>(
            Guid.NewGuid().ToString(), zero.HandleAsync, c => c.WithConsumerDispatcherConcurrency(0), cts.Token
        ))
        {
            await bus.PubSub.PublishBatchAsync(messages, cts.Token);

            await sequential.WaitAllReceivedAsync(cts.Token);
            await parallel.WaitAllReceivedAsync(cts.Token);
            await inherited.WaitAllReceivedAsync(cts.Token);
            await zero.WaitAllReceivedAsync(cts.Token);
        }

        sequential.MaxInFlight.Should().Be(1);
        sequential.ReceivedMessages.Should().Equal(messages);

        parallel.MaxInFlight.Should().Be(ParallelDispatcherConcurrency);
        parallel.ReceivedMessages.Should().BeEquivalentTo(messages);

        inherited.MaxInFlight.Should().Be(ConnectionDispatcherConcurrency, "a subscription without its own value keeps the connection-wide concurrency");
        inherited.ReceivedMessages.Should().BeEquivalentTo(messages);

        zero.MaxInFlight.Should().Be(ConnectionDispatcherConcurrency, "0 is not forwarded, as in the AutoSubscriber attribute, so the connection-wide concurrency applies");
        zero.ReceivedMessages.Should().BeEquivalentTo(messages);
    }

    private sealed class InFlightTracker
    {
        private readonly MessagesSink messagesSink;
        private readonly int expectedMaxInFlight;
        private readonly TaskCompletionSource<object> expectedMaxInFlightReached = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object locker = new();
        private int inFlight;
        private int maxInFlight;

        public InFlightTracker(int messagesCount, int expectedMaxInFlight)
        {
            messagesSink = new MessagesSink(messagesCount);
            this.expectedMaxInFlight = expectedMaxInFlight;
        }

        public int MaxInFlight
        {
            get
            {
                lock (locker)
                {
                    return maxInFlight;
                }
            }
        }

        public IReadOnlyList<Message> ReceivedMessages => messagesSink.ReceivedMessages;

        public async Task HandleAsync(Message message, CancellationToken cancellationToken)
        {
            lock (locker)
            {
                inFlight++;
                maxInFlight = Math.Max(maxInFlight, inFlight);
                if (inFlight >= expectedMaxInFlight)
                    expectedMaxInFlightReached.TrySetResult(null);
            }

            // Holding every message for a while exposes any concurrency above the expected one, and holding it until the
            // expected concurrency is reached keeps the test independent of how fast the broker delivers
            await Task.WhenAll(
                Task.Delay(HandlingTime, cancellationToken),
                Task.WhenAny(expectedMaxInFlightReached.Task, Task.Delay(MaxWaitForExpectedConcurrency, cancellationToken))
            );

            lock (locker)
            {
                inFlight--;
            }

            messagesSink.Receive(message);
        }

        public Task WaitAllReceivedAsync(CancellationToken cancellationToken) =>
            messagesSink.WaitAllReceivedAsync(cancellationToken);
    }
}
