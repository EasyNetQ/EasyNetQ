using System;
using System.Collections.Generic;
using EasyNetQ.Consumer;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
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
                var basicGetResult = channel.BasicGet(parameters.QueueName, parameters.Purge);
                if (basicGetResult == null) break; // no more messages on the queue

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
}
