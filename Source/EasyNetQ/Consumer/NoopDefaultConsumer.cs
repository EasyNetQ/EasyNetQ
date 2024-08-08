using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasyNetQ.Consumer;

internal sealed class NoopDefaultConsumer : IAsyncBasicConsumer
{
    internal static readonly NoopDefaultConsumer Instance = new();

    private NoopDefaultConsumer() { }

    public IChannel Channel => throw new NotSupportedException();

    public static event AsyncEventHandler<ConsumerEventArgs> ConsumerCancelled
    {
        add { }
        remove { }
    }

    public Task HandleBasicCancelAsync(string consumerTag) => Task.CompletedTask;

    public Task HandleBasicCancelOkAsync(string consumerTag) => Task.CompletedTask;

    public Task HandleBasicConsumeOkAsync(string consumerTag) => Task.CompletedTask;

    public Task HandleBasicDeliverAsync(
        string consumerTag,
        ulong deliveryTag,
        bool redelivered,
        string exchange,
        string routingKey,
        IReadOnlyBasicProperties properties,
        ReadOnlyMemory<byte> body
    ) => Task.CompletedTask;

    public Task HandleChannelShutdownAsync(object channel, ShutdownEventArgs reason) => Task.CompletedTask;
}
