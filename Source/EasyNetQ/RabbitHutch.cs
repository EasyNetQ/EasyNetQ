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
                connectionValues.VirtualHost,
                connectionValues.UserName, 
                connectionValues.Password, 
                logger);
        }

        /// <summary>
        /// Creates a new instance of RabbitBus
        /// </summary>
        /// <param name="hostName">
        /// The RabbitMQ broker.
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
        /// <param name="logger">
        /// The logger to use.
        /// </param>
        /// <returns>
        /// A new RabbitBus instance.
        /// </returns>
        public static IBus CreateBus(string hostName, string virtualHost, string username, string password, IEasyNetQLogger logger)
        {
            if(hostName == null)
            {
                throw new ArgumentNullException("hostName");
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

            var connectionFactory = new ConnectionFactoryWrapper(new ConnectionFactory
            {
                HostName = hostName,
                VirtualHost = virtualHost,
                UserName = username,
                Password = password
            });

            var serializer = new JsonSerializer();

            var consumerErrorStrategy = new DefaultConsumerErrorStrategy(connectionFactory, serializer, logger);

            var conventions = new Conventions();

            return new RabbitBus(
                TypeNameSerializer.Serialize,
                serializer,
                new QueueingConsumerFactory(logger, consumerErrorStrategy),
                connectionFactory,
                logger, 
                CorrelationIdGenerator.GetCorrelationId,
                conventions);
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