using EasyNetQ.Consumer;
using EasyNetQ.Events;
using EasyNetQ.Pipeline;
using EasyNetQ.Topology;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.ConsumeTests;

public class When_an_ack_fails
{
    private const int AckFailedWillRetry = 307;
    private const int UnexpectedAckFailure = 308;

    [Fact]
    public async Task Should_log_a_cancelled_ack_at_shutdown_as_retried()
    {
        using var shutdown = new CancellationTokenSource();
        var logger = await DeliverAsync(
            ct => ct.IsCancellationRequested ? Task.FromCanceled(ct) : Task.CompletedTask,
            shutdown,
            cancelBeforeAck: true
        );

        logger.Entries.Should().ContainSingle(e => e.EventId.Id == AckFailedWillRetry)
            .Which.Level.Should().Be(LogLevel.Information);
        logger.Entries.Should().NotContain(e => e.EventId.Id == UnexpectedAckFailure);
    }

    [Fact]
    public async Task Should_log_an_unexpected_ack_failure_as_error()
    {
        using var shutdown = new CancellationTokenSource();
        var logger = await DeliverAsync(_ => throw new InvalidOperationException("boom"), shutdown, cancelBeforeAck: false);

        logger.Entries.Should().ContainSingle(e => e.EventId.Id == UnexpectedAckFailure)
            .Which.Level.Should().Be(LogLevel.Error);
    }

    [Fact]
    public async Task Should_log_a_cancellation_not_requested_by_the_consumer_as_error()
    {
        using var shutdown = new CancellationTokenSource();
        var logger = await DeliverAsync(_ => throw new TaskCanceledException(), shutdown, cancelBeforeAck: false);

        logger.Entries.Should().ContainSingle(e => e.EventId.Id == UnexpectedAckFailure);
    }

    private static async Task<RecordingLogger> DeliverAsync(
        Func<CancellationToken, Task> ack, CancellationTokenSource shutdown, bool cancelBeforeAck
    )
    {
        var channel = Substitute.For<IChannel>();
        channel.BasicAckAsync(Arg.Any<ulong>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(ci => new ValueTask(ack(ci.ArgAt<CancellationToken>(2))));

        await using var services = new ServiceCollection().BuildServiceProvider();
        var consumerContext = new ConsumerContext(new ChannelContext(new ConnectionContext("test", services)), "the_queue")
        {
            MessagePipeline = context =>
            {
                context.Ack = AckDecision.Ack;
                if (cancelBeforeAck)
                    shutdown.Cancel();
                return default;
            }
        };
        var logger = new RecordingLogger();
        await using var consumer = new AsyncBasicConsumer(
            logger, channel, new Queue("the_queue"), false, Substitute.For<IEventBus>(), consumerContext
        );

        await consumer.HandleBasicDeliverAsync(
            "the_tag", 1, false, "the_exchange", "the_key", new BasicProperties(), "{}"u8.ToArray(), shutdown.Token
        );
        return logger;
    }

    private sealed class RecordingLogger : ILogger<InternalConsumer>
    {
        public List<(LogLevel Level, EventId EventId)> Entries { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, eventId));
    }
}
