using System;
using RabbitMQ.Client;

namespace EasyNetQ
{
    /// <summary>
    /// Does poor man's dependency injection. Supplies default instances of services required by
    /// RabbitBus.
    /// </summary>
    public static class RabbitHutch
    {
        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="applicationId">
        /// Identifies the application. If two instances of an application share the same applicationId
        /// messages for a subscribed type will be delivered to them by AMQP in turn (round-robin). If 
        /// the applicationId is different each instance will get every message.
        /// </param>
        /// <param name="hostName">
        /// The RabbitMQ broker. To use the default Virtual Host, simply use the server name, e.g. 'localhost'.
        /// To identify the Virtual Host use the following scheme: 'hostname/virtualhost' e.g. 'localhost/myvhost'
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateRabbitBus(string applicationId, string hostName)
        {
            if(hostName == null)
            {
                throw new ArgumentNullException("hostName");
            }

            var rabbitHost = GetRabbitHost(hostName);

            var connectionFactory = new ConnectionFactory
            {
                HostName = rabbitHost.HostName,
                VirtualHost = rabbitHost.VirtualHost
            };
            var connection = connectionFactory.CreateConnection();

            SubsriberNameFromDelegate subsriberNameFromDelegate = 
                @delegate => applicationId + "_" + DelegateNameBuilder.CreateNameFrom(@delegate);

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                subsriberNameFromDelegate,
                new BinarySerializer(),
                connection);
        }

        public static RabbitHost GetRabbitHost(string hostName)
        {
            var hostNameParts = hostName.Split('/');
            if (hostNameParts.Length > 2)
            {
                throw new EasyNetQException(@"hostname has too many parts, expecting '<server>/<vhost>' but was: '{0}'", 
                    hostName);
            }

            return new RabbitHost
            {
                HostName = hostNameParts[0],
                VirtualHost = hostNameParts.Length==1 ? "/" : hostNameParts[1]
            };
        }

        public struct RabbitHost
        {
            public string HostName;
            public string VirtualHost;
        }
    }
}