using RabbitMQ.Client;

namespace EasyNetQ.Tests.Mocking;

public sealed record PublishedMessage(string Exchange, string RoutingKey, bool Mandatory, RabbitMQ.Client.BasicProperties Properties, byte[] Body);

/// <summary>
///     Records every publish on a channel substitute, arguments copied at call time: the body is a pooled buffer
///     returned once the publish completes, so <c>Received()</c> only keeps a view of bytes other tests may overwrite
/// </summary>
public sealed class PublishRecorder
{
    private readonly List<PublishedMessage> messages = new();

    public PublishRecorder(IChannel channel)
    {
        channel.When(x => x.BasicPublishAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<RabbitMQ.Client.BasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>()
            ))
            .Do(call =>
            {
                var message = new PublishedMessage(
                    call.ArgAt<string>(0),
                    call.ArgAt<string>(1),
                    call.ArgAt<bool>(2),
                    new RabbitMQ.Client.BasicProperties(call.ArgAt<RabbitMQ.Client.BasicProperties>(3)),
                    call.ArgAt<ReadOnlyMemory<byte>>(4).ToArray()
                );
                lock (messages)
                    messages.Add(message);
            });
    }

    public IReadOnlyList<PublishedMessage> Messages
    {
        get
        {
            lock (messages)
                return messages.ToArray();
        }
    }
}
