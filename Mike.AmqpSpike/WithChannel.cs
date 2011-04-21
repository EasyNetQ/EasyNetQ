using System;
using RabbitMQ.Client;

namespace Mike.AmqpSpike
{
    public class WithChannel
    {
        public static void Do(Action<IModel> modelAction)
        {
            var connectionFactory = new ConnectionFactory { HostName = "localhost" };
            using (var connection = connectionFactory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                modelAction(channel);
            }            
        }
    }
}