using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer;

internal sealed class NoopDefaultConsumer : IAsyncBasicConsumer
{
    internal static readonly NoopDefaultConsumer Instance = new();

    private NoopDefaultConsumer() { }

    public IChannel Channel => throw new NotSupportedException();

    public event AsyncEventHandler<ConsumerEventArgs> ConsumerCancelled
    {
        add { }
        remove { }
    }

    public Task HandleBasicCancelAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task HandleBasicCancelOkAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task HandleBasicConsumeOkAsync(string consumerTag, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body,
        CancellationToken cancellationToken = default
    ) => Task.CompletedTask;


    public Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason) => Task.CompletedTask;
}
