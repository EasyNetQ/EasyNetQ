using EasyNetQ.Consumer;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe;

public interface IQueueRetrieval
{
    IEnumerable<HosepipeMessage> GetMessagesFromQueue(QueueParameters parameters);
}

public class QueueRetrieval : IQueueRetrieval
{
    private readonly IErrorMessageSerializer errorMessageSerializer;

    public QueueRetrieval(IErrorMessageSerializer errorMessageSerializer)
    {
        this.errorMessageSerializer = errorMessageSerializer;
    }

    public IEnumerable<HosepipeMessage> GetMessagesFromQueue(QueueParameters parameters)
    {
        using var connection = HosepipeConnection.FromParameters(parameters);
        using var channel = connection.CreateModel();

        try
        {
            channel.QueueDeclarePassive(parameters.QueueName);
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
                basicGetResult = channel.BasicGet(parameters.QueueName, false);
                if (basicGetResult == null) break; // no more messages on the queue

                if (parameters.Purge)
                {
                    channel.BasicAck(basicGetResult.DeliveryTag, false);
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                throw;
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
