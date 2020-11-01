using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe
{
    public static class HosepipeConnection
    {
        public static IConnection FromParameters(QueueParameters parameters)
        {
            var connectionFactory = new ConnectionFactory
            {
                HostName = parameters.HostName,
                VirtualHost = parameters.VHost,
                UserName = parameters.Username,
                Password = parameters.Password,
                Port = parameters.HostPort
            };
            try
            {
                return connectionFactory.CreateConnection();
            }
            catch (BrokerUnreachableException)
            {
                throw new EasyNetQHosepipeException(string.Format(
                    "The broker at '{0}{2}' VirtualHost '{1}', is unreachable. This message can also be caused " +
                    "by incorrect credentials.",
                    parameters.HostName,
                    parameters.VHost,
                    parameters.HostPort == -1 ? string.Empty : ":" + parameters.HostPort));
            }
        }
    }
}
