using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public class QueueInsertion : IQueueInsertion
    {
        public void PublishMessagesToQueue(IEnumerable<string> messages, QueueParameters parameters)
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
                    return;
                }

                foreach (var message in messages)
                {
                    var body = Encoding.UTF8.GetBytes(message);
                    var properties = channel.CreateBasicProperties();

                    // take advantage of the fact that every AMQP queue binds to the default ("")
                    // queue using its name as the routing key
                    channel.BasicPublish("", parameters.QueueName, properties, body);
                }
            }                        
        }
    }
}