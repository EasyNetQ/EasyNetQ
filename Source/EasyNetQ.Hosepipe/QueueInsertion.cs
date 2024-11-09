using EasyNetQ.Consumer;
using RabbitMQ.Client;

namespace EasyNetQ.Hosepipe;

public class QueueInsertion : IQueueInsertion
{
    private readonly IErrorMessageSerializer errorMessageSerializer;

    public QueueInsertion(IErrorMessageSerializer errorMessageSerializer)
    {
        this.errorMessageSerializer = errorMessageSerializer;
    }

    public async Task PublishMessagesToQueueAsync(
        IAsyncEnumerable<HosepipeMessage> messages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var connection = await HosepipeConnection.FromParametersAsync(parameters, cancellationToken);
        using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken: cancellationToken);

        await foreach (var message in messages)
        {
            var body = errorMessageSerializer.Deserialize(message.Body);

            var properties = new BasicProperties();
            message.Properties.CopyTo(properties);

            var queueName = string.IsNullOrEmpty(parameters.QueueName)
                ? message.Info.Queue
                : parameters.QueueName;

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: queueName,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken
            );

            using var cts = new CancellationTokenSource(parameters.ConfirmsTimeout);
            // await channel.WaitForConfirmsOrDieAsync(cts.Token);
        }
    }
}
