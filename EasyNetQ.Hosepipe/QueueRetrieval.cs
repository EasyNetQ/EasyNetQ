using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public interface IQueueRetreival {
        IEnumerable<string> GetMessagesFromQueue(QueueParameters queueParameters);
    }

    public class QueueRetreival : IQueueRetreival
    {
        public IEnumerable<string> GetMessagesFromQueue(QueueParameters queueParameters)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = queueParameters.HostName,
                VirtualHost = queueParameters.VHost,
                UserName = queueParameters.Username,
                Password = queueParameters.Password
            };

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                try
                {
                    channel.QueueDeclarePassive(queueParameters.QueueName);
                }
                catch (OperationInterruptedException exception)
                {
                    Console.WriteLine(exception.Message);
                    yield break;
                }

                var count = 0;
                while (++count < queueParameters.NumberOfMessagesToRetrieve)
                {
                    var basicGetResult = channel.BasicGet(queueParameters.QueueName, noAck: queueParameters.Purge);
                    if (basicGetResult == null) break; // no more messages on the queue

                    yield return Encoding.UTF8.GetString(basicGetResult.Body);
                }
            }            
        } 
    }
}