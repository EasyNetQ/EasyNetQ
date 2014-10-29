using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public interface IQueueRetreival {
        IEnumerable<HosepipeMessage> GetMessagesFromQueue(QueueParameters parameters);
    }

    public class QueueRetreival : IQueueRetreival
    {
        public IEnumerable<HosepipeMessage> GetMessagesFromQueue(QueueParameters parameters)
        {
            using (var connection = HosepipeConnection.FromParamters(parameters))
            using (var channel = connection.CreateModel())
            {
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
                    var basicGetResult = channel.BasicGet(parameters.QueueName, noAck: parameters.Purge);
                    if (basicGetResult == null) break; // no more messages on the queue

                    var properties = new MessageProperties(basicGetResult.BasicProperties);
                    var info = new MessageReceivedInfo(
                        "hosepipe",
                        basicGetResult.DeliveryTag,
                        basicGetResult.Redelivered,
                        basicGetResult.Exchange,
                        basicGetResult.RoutingKey,
                        parameters.QueueName);

                    yield return new HosepipeMessage(Encoding.UTF8.GetString(basicGetResult.Body), properties, info);
                }
            }            
        } 
    }
}