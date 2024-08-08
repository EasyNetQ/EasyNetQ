using EasyNetQ.Consumer;
using EasyNetQ.SystemMessages;
using RabbitMQ.Client;

namespace EasyNetQ.Hosepipe;

public class ErrorRetry : IErrorRetry
{
    private readonly ISerializer serializer;

    private readonly IErrorMessageSerializer errorMessageSerializer;

    public ErrorRetry(ISerializer serializer, IErrorMessageSerializer errorMessageSerializer)
    {
        this.serializer = serializer;
        this.errorMessageSerializer = errorMessageSerializer;
    }

    public async Task RetryErrorsAsync(IAsyncEnumerable<HosepipeMessage> rawErrorMessages, QueueParameters parameters)
    {
        using var connection = await HosepipeConnection.FromParametersAsync(parameters);
        using var channel = await connection.CreateChannelAsync();

        await channel.ConfirmSelectAsync();

        await foreach (var rawErrorMessage in rawErrorMessages)
        {
            var error = (Error)serializer.BytesToMessage(typeof(Error), errorMessageSerializer.Deserialize(rawErrorMessage.Body));
            var properties = new BasicProperties();
            error.BasicProperties.CopyTo(properties);
            var body = errorMessageSerializer.Deserialize(error.Message).AsMemory();

            await channel.BasicPublishAsync(
                exchange: "",
                routingKey: error.Queue,
                mandatory: true,
                basicProperties: properties,
                body: body
            );

            await channel.WaitForConfirmsOrDieAsync();
        }
    }
}
