using System;
using System.Configuration;
using EasyNetQ.Loggers;
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
        /// Creates a new instance of RabbitBus.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;virtualHost=/;username=guest;password=guest
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string connectionString)
        {
            return CreateBus(connectionString, new ConsoleLogger());
        }

        /// <summary>
        /// Creates a new instance of RabbitBus.
        /// </summary>
        /// <param name="connectionString">
        /// The EasyNetQ connection string. Example:
        /// host=192.168.1.1;virtualHost=MyVirtualHost;username=MyUsername;password=MyPassword
        /// 
        /// The following default values will be used if not specified:
        /// host=localhost;virtualHost=/;username=guest;password=guest
        /// </param>
        /// <param name="logger">
        /// An implementation of IEasyNetQLogger to send the log output to.
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string connectionString, IEasyNetQLogger logger)
        {
            if(connectionString == null)
            {
                throw new ArgumentNullException("connectionString");
            }
            if(logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            var connectionValues = new ConnectionString(connectionString);

            return CreateBus(
                connectionValues.Host, 
                connectionValues.Port, 
                connectionValues.VirtualHost,
                connectionValues.UserName, 
                connectionValues.Password, 
                connectionValues.RequestedHeartbeat,
                logger);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker.
        /// </param>
        /// <param name="hostPort">
        /// The RabbitMQ broker port.
        /// </param>
        /// <param name="virtualHost">
        /// The RabbitMQ virtualHost.
        /// </param>
        /// <param name="username">
        /// The username to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="password">
        /// The password to use to connect to the RabbitMQ broker.
        /// </param>
        /// <param name="requestedHeartbeat">
        /// The heartbeat to set for the connection. Null for no heartbeat.
        /// </param>
        /// <param name="logger">
        /// The logger to use.
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string hostName, string hostPort, string virtualHost, string username, string password, string requestedHeartbeat, IEasyNetQLogger logger)
        {
            if(hostName == null)
            {
                throw new ArgumentNullException("hostName");
            }
            if (hostPort == null)
            {
                throw new ArgumentNullException("hostPort");
            }
            if(virtualHost == null)
            {
                throw new ArgumentNullException("virtualHost");
            }
            if(username == null)
            {
                throw new ArgumentNullException("username");
            }
            if(password == null)
            {
                throw new ArgumentNullException("password");
            }
            if(logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            var port = 0;
            if (!Int32.TryParse(hostPort, out port))
            {
                throw new FormatException("hostPort must be a valid 32-bit interger.");
            }

            var rabbitConnectionFactory = new ConnectionFactory
                {
                    HostName = hostName, 
                    Port = port, 
                    VirtualHost = virtualHost, 
                    UserName = username, 
                    Password = password
                };

            if(requestedHeartbeat != null)
            {
                ushort heartbeat;
                if (ushort.TryParse(requestedHeartbeat, out heartbeat))
                {
                    rabbitConnectionFactory.RequestedHeartbeat = heartbeat;
                }
                else
                {
                    throw new FormatException("requestedHeartbeat must be a valid 32-bit interger.");
                }
            }

            var connectionFactory = new ConnectionFactoryWrapper(rabbitConnectionFactory);

            var serializer = new JsonSerializer();

            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, logger);

            var conventions = new Conventions();

            var advancedBus = new RabbitAdvancedBus(
                connectionFactory,
                TypeNameSerializer.Serialize,
                serializer,
                new QueueingConsumerFactory(logger, consumerErrorStrategy), 
                logger,
                CorrelationIdGenerator.GetCorrelationId, 
                conventions);

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                logger, 
                conventions,
                advancedBus);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// The RabbitMQ broker is defined in the connection string named 'rabbit'
        /// </summary>
        /// <returns></returns>
        public static IBus CreateBus()
        {
            var rabbitConnectionString = ConfigurationManager.ConnectionStrings["rabbit"];
            if (rabbitConnectionString == null)
            {
                throw new EasyNetQException(
                    "Could not find a connection string for RabbitMQ. " +
                    "Please add a connection string in the <ConnectionStrings> secion" +
                    "of the application's configuration file. For example: " +
                    "<add name=\"rabbit\" connectionString=\"host=localhost\" />");
            }

            return CreateBus(rabbitConnectionString.ConnectionString);
        }
    }
}