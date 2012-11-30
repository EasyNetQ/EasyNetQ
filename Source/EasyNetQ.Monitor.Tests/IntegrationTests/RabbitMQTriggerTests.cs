using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using EasyNetQ.Management.Client;
using EasyNetQ.Management.Client.Model;
using RabbitMQ.Client;

namespace EasyNetQ.Monitor.Tests.IntegrationTests
{
    public class RabbitMqTriggerTests
    {
        public void CreateOver100Connections()
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            var connections = new List<IConnection>();
            for (var i = 0; i < 101; i++)
            {
                var connection = connectionFactory.CreateConnection();
                connections.Add(connection);
            }

            Console.Out.WriteLine("Created 101 connections");
            Thread.Sleep(TimeSpan.FromSeconds(10));

            Console.Out.WriteLine("Disposing connections");
            foreach (var connection in connections)
            {
                connection.Dispose();
            }
            Console.Out.WriteLine("Connections disposed");
        }

        public void CreateOver100Channels()
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            var connection = connectionFactory.CreateConnection();
            for (int i = 0; i < 101; i++)
            {
                connection.CreateModel();
            }

            Console.Out.WriteLine("Created 101 connection");
            Thread.Sleep(TimeSpan.FromSeconds(10));


            Console.Out.WriteLine("Disposing connection");
            connection.Dispose();
            Console.Out.WriteLine("Connection disposed");
        }

        public void CreateOver100Messages()
        {
            const string queueName = "break_me_mofo";

            var connectionFactory = new ConnectionFactory
            {
                HostName = "localhost"
            };

            var message = Encoding.UTF8.GetBytes("Hello World!");

            using(var connection = connectionFactory.CreateConnection())
            {
                using (var model = connection.CreateModel())
                {
                    model.QueueDeclare(queueName, true, false, false, null);
                }

                using(var model = connection.CreateModel())
                {
                    for (int i = 0; i < 101; i++)
                    {
                        model.BasicPublish("", queueName, model.CreateBasicProperties(), message);
                    }
                }

                Console.Out.WriteLine("Created 101 messages in queue");
                Thread.Sleep(TimeSpan.FromSeconds(10));

                using(var model = connection.CreateModel())
                {
                    model.QueueDelete(queueName);
                }
            }
        }
    }
}