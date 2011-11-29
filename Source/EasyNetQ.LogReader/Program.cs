using System;
using System.Text;
using RabbitMQ.Client;

namespace EasyNetQ.LogReader
{
    public class Program
    {
        private const string amqpLogExchange = "amq.rabbitmq.log";
        private const string logReaderQueue = "EasyNetQ_LogReader";

        static void Main(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Console.WriteLine("EasyNetQ.LogReader");
                Console.WriteLine("Usage:");
                Console.WriteLine("EasyNetQ.LogReader.exe <rabbitMqServer> [<vhost>]");
                return;
            }

            var connectionFactory = new ConnectionFactory
            {
                HostName = args[0],
                VirtualHost = args.Length == 2 ? args[1] : "/"
            };

            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(
                    logReaderQueue, // name
                    false,          // durable
                    true,           // exclusive
                    true,           // autoDelete
                    null);          // arguments

                channel.QueueBind(logReaderQueue, amqpLogExchange, "*");

                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(logReaderQueue, false, consumer);

                Console.WriteLine("EasyNetQ.LogReader");
                Console.WriteLine("Listening to log");

                while (true)
                {
                    try
                    {
                        var e = (RabbitMQ.Client.Events.BasicDeliverEventArgs) consumer.Queue.Dequeue();
                        var logMessage = Encoding.UTF8.GetString(e.Body);

                        Console.WriteLine(logMessage);

                        channel.BasicAck(e.DeliveryTag, false);
                    }
                    catch (Exception exception)
                    {
                        // The consumer was removed, either through
                        // channel or connection closure, or through the
                        // action of IModel.BasicCancel().

                        Console.WriteLine(exception);
                        Console.WriteLine();
                        Console.WriteLine("Connection closed.");

                        break;
                    }
                }
            }
        }
    }
}
