using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
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

    public async Task RetryErrorsAsync(
        IAsyncEnumerable<HosepipeMessage> rawErrorMessages,
        QueueParameters parameters,
        CancellationToken cancellationToken = default)
    {
        using var connection = await HosepipeConnection.FromParametersAsync(parameters, cancellationToken);
        using var channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), cancellationToken);

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
                body: body,
                cancellationToken: cancellationToken
            );

            // await channel.WaitForConfirmsOrDieAsync(cancellationToken);
        }
    }
}
