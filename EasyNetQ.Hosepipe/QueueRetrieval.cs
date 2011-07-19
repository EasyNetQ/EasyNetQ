using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public class QueueRetreival
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
                    if (basicGetResult == null) break;

                    yield return Encoding.UTF8.GetString(basicGetResult.Body);
                }
            }            
        } 
    }

    public class QueueParameters
    {
        public string HostName { get; set; }
        public string VHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string QueueName { get; set; }
        public bool Purge { get; set; }
        public int NumberOfMessagesToRetrieve { get; set; }

        public QueueParameters()
        {
            // set some defaults
            HostName = "localhost";
            VHost = "/";
            Username = "guest";
            Password = "guest";
            Purge = false;
            NumberOfMessagesToRetrieve = 1000;
        }
    }

    public class Play
    {
        public void TryGetMessagesFromQueue()
        {
            const string queue = "test_EasyNetQ_Tests_MyMessage:EasyNetQ_Tests";

            var queueRetrieval = new QueueRetreival();
            var parameters = new QueueParameters
            {
                QueueName = queue,
                Purge = true
            };

            foreach (var message in queueRetrieval.GetMessagesFromQueue(parameters))
            {
                Console.Out.WriteLine("message = {0}", message);
            }
        }
    }

}