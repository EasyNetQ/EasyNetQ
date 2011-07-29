using RabbitMQ.Client;

namespace EasyNetQ.Hosepipe
{
    public class HosepipeConnection
    {
        public static IConnection FromParamters(QueueParameters parameters)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = parameters.HostName,
                VirtualHost = parameters.VHost,
                UserName = parameters.Username,
                Password = parameters.Password
            };
            return connectionFactory.CreateConnection();
        } 
    }
}