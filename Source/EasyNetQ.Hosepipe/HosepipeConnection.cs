using System.Threading.Tasks;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace EasyNetQ.Hosepipe;

public static class HosepipeConnection
{
    public static async Task<IConnection> FromParametersAsync(QueueParameters parameters, CancellationToken cancellationToken = default)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = parameters.HostName,
            VirtualHost = parameters.VHost,
            UserName = parameters.Username,
            Password = parameters.Password,
            Port = parameters.HostPort,
        };

        if (parameters.Ssl)
        {
            connectionFactory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = parameters.HostName
            };
        }

        try
        {
            return await connectionFactory.CreateConnectionAsync(cancellationToken);
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
