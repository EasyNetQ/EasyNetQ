using EasyNetQ.Consumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe;

public interface IQueueRetrieval
{
    IAsyncEnumerable<HosepipeMessage> GetMessagesFromQueueAsync(QueueParameters parameters);
}

public class QueueRetrieval : IQueueRetrieval
{
    private readonly IErrorMessageSerializer errorMessageSerializer;

    public QueueRetrieval(IErrorMessageSerializer errorMessageSerializer)
    {
        this.errorMessageSerializer = errorMessageSerializer;
    }

    public async IAsyncEnumerable<HosepipeMessage> GetMessagesFromQueueAsync(QueueParameters parameters)
    {
        using var connection = await HosepipeConnection.FromParametersAsync(parameters);
        using var channel = await connection.CreateChannelAsync();

        try
        {
            await channel.QueueDeclarePassiveAsync(parameters.QueueName);
        }
        catch (OperationInterruptedException exception)
        {
            Console.WriteLine(exception.Message);
            yield break;
        }

        var count = 0;
        while (count++ < parameters.NumberOfMessagesToRetrieve)
        {
            BasicGetResult basicGetResult;
            try
            {
                basicGetResult = await channel.BasicGetAsync(parameters.QueueName, false);
                if (basicGetResult == null) break; // no more messages on the queue

                if (parameters.Purge)
                {
                    await channel.BasicAckAsync(basicGetResult.DeliveryTag, false);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                yield break;
            }

            var properties = new MessageProperties(basicGetResult.BasicProperties);
            var info = new MessageReceivedInfo(
                "hosepipe",
                basicGetResult.DeliveryTag,
                basicGetResult.Redelivered,
                basicGetResult.Exchange,
                basicGetResult.RoutingKey,
                parameters.QueueName
            );

            yield return new HosepipeMessage(errorMessageSerializer.Serialize(basicGetResult.Body.ToArray()), properties, info);
        }
    }
}
