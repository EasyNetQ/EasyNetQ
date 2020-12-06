using System;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ
{
    /// <summary>
    ///     Contains various settings of a connection and more
    /// </summary>
    public class ConnectionConfiguration
    {
        /// <summary>
        ///     Default AMQP port
        /// </summary>
        public const int DefaultPort = 5672;

        /// <summary>
        ///     Default secured AMQP port
        /// </summary>
        public const int DefaultAmqpsPort = 5671;

        /// <summary>
        /// </summary>
        public ConnectionConfiguration()
        {
            Port = DefaultPort;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = TimeSpan.FromSeconds(10);
            Timeout = TimeSpan.FromSeconds(10);
            PublisherConfirms = false;
            PersistentMessages = true;
            ConnectIntervalAttempt = TimeSpan.FromSeconds(5);
            MandatoryPublish = false;

            // prefetchCount determines how many messages will be allowed in the local in-memory queue
            // setting to zero makes this infinite, but risks an out-of-memory exception.
            // set to 50 based on this blog post:
            // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
            PrefetchCount = 50;
            AuthMechanisms = new List<IAuthMechanismFactory> {new PlainMechanismFactory()};

            Hosts = new List<HostConfiguration>();

            Ssl = new SslOption();
            ClientProperties = new Dictionary<string, object>();
        }

        /// <summary>
        ///     Port used to connect to the broker
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        ///     Virtual host to connect to
        /// </summary>
        public string VirtualHost { get; set; }

        /// <summary>
        ///     UserName used to connect to the broker
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        ///     Password used to connect to the broker
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        ///     Heartbeat interval (default is 10 seconds)
        /// </summary>
        public TimeSpan RequestedHeartbeat { get; set; }

        /// <summary>
        ///     Prefetch count (default is 50)
        /// </summary>
        public ushort PrefetchCount { get; set; }

        /// <summary>
        ///     Client properties to be sent to the broker
        /// </summary>
        public IDictionary<string, object> ClientProperties { get; }

        /// <summary>
        ///     List of hosts to use for the connection
        /// </summary>
        public IList<HostConfiguration> Hosts { get; set; }

        /// <summary>
        ///     TLS options for the connection.
        /// </summary>
        public SslOption Ssl { get; }

        /// <summary>
        ///     Operations timeout (default is 10s)
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        ///     Enables publisher confirms (default is false)
        /// </summary>
        public bool PublisherConfirms { get; set; }

        /// <summary>
        ///     Enables persistent messages (default is true)
        /// </summary>
        public bool PersistentMessages { get; set; }

        /// <summary>
        ///     Allows to override default product value
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        ///     Allows to override default platform value
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        ///     Name to be used for connection
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Auth mechanisms to use
        /// </summary>
        public IList<IAuthMechanismFactory> AuthMechanisms { get; set; }

        /// <summary>
        ///     Interval between reconnection attempts. (default is 5s)
        /// </summary>
        public TimeSpan ConnectIntervalAttempt { get; set; }

        /// <summary>
        ///     Enables mandatory flag for publish (default is false)
        /// </summary>
        public bool MandatoryPublish { get; set; }
    }

    /// <summary>
    ///     Represents a host configuration
    /// </summary>
    public class HostConfiguration
    {
        /// <summary>
        ///     Address of the host
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        ///     Port of the host
        /// </summary>
        public ushort Port { get; set; }

        /// <summary>
        ///     TSL configuration of the host
        /// </summary>
        public SslOption Ssl { get; } = new SslOption();
    }
}
