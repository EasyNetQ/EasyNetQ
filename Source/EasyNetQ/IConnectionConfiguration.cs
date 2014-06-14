using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConnectionConfiguration
    {
        ushort Port { get; }
        string VirtualHost { get; }
        string UserName { get; }
        string Password { get; }

        /// <summary>
        /// Heartbeat interval seconds. (default is 10)
        /// </summary>
        ushort RequestedHeartbeat { get; }
        ushort PrefetchCount { get; }
        Uri AMQPConnectionString { get; }
        IDictionary<string, object> ClientProperties { get; }
        
        IEnumerable<IHostConfiguration> Hosts { get; }

        SslOption Ssl { get; }

        /// <summary>
        /// Operation timeout seconds. (default is 10)
        /// </summary>
        ushort Timeout { get; }

        bool PublisherConfirms { get; }
        bool PersistentMessages { get; set; }

        bool CancelOnHaFailover { get; }
    }

    public interface IHostConfiguration
    {
        string Host { get; }
        ushort Port { get; }
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        private const int DefaultPort = 5672;
        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }
        public ushort PrefetchCount { get; set; }
        public Uri AMQPConnectionString { get; set; }
        public IDictionary<string, object> ClientProperties { get; private set; } 

        public IEnumerable<IHostConfiguration> Hosts { get; set; }
        public SslOption Ssl { get; private set; }
        public ushort Timeout { get; set; }
        public bool PublisherConfirms { get; set; }
        public bool PersistentMessages { get; set; }
        public bool CancelOnHaFailover { get; set; }
        public string Product { get; set; }
        public string Platform { get; set; }

        public ConnectionConfiguration()
        {
            // set default values
            Port = DefaultPort;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = 10;
            Timeout = 10; // seconds
            PublisherConfirms = false;
            PersistentMessages = true;
            CancelOnHaFailover = false;

            // prefetchCount determines how many messages will be allowed in the local in-memory queue
            // setting to zero makes this infinite, but risks an out-of-memory exception.
            // set to 50 based on this blog post:
            // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
            PrefetchCount = 50;
            
            Hosts = new List<IHostConfiguration>();

            Ssl = new SslOption();
        }

        private void SetDefaultClientProperties(IDictionary<string, object> clientProperties)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var applicationNameAndPath = Environment.GetCommandLineArgs()[0];
            var applicationName = Path.GetFileName(applicationNameAndPath);
            var applicationPath = Path.GetDirectoryName(applicationNameAndPath);
            var hostname = Environment.MachineName;
            var product = Product ?? applicationName;
            var platform = Platform ?? hostname;

            clientProperties.Add("client_api", "EasyNetQ");
            clientProperties.Add("product", product);
            clientProperties.Add("platform", platform);
            clientProperties.Add("version", version);
            clientProperties.Add("easynetq_version", version);
            clientProperties.Add("application", applicationName);
            clientProperties.Add("application_location", applicationPath);
            clientProperties.Add("machine_name", hostname);
            clientProperties.Add("user", UserName);
            clientProperties.Add("connected", DateTime.Now.ToString("MM/dd/yy HH:mm:ss"));
            clientProperties.Add("requested_heartbeat", RequestedHeartbeat.ToString());
            clientProperties.Add("timeout", Timeout.ToString());
            clientProperties.Add("publisher_confirms", PublisherConfirms.ToString());
            clientProperties.Add("persistent_messages", PersistentMessages.ToString());
        }

        public void Validate()
        {
            if (AMQPConnectionString != null)
            {
                if(Port == DefaultPort && AMQPConnectionString.Port > 0) 
                        Port = (ushort) AMQPConnectionString.Port;
                Hosts = Hosts.Concat(new[] {new HostConfiguration {Host = AMQPConnectionString.Host}});
            }
            if (!Hosts.Any())
            {
                throw new EasyNetQException("Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");
            }
            foreach (var hostConfiguration in Hosts)
            {
                if (hostConfiguration.Port == 0)
                {
                    ((HostConfiguration)hostConfiguration).Port = Port;
                }
            }

            ClientProperties = new Dictionary<string, object>();
            SetDefaultClientProperties(ClientProperties);
        }
    }

    public class HostConfiguration : IHostConfiguration
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
    }
}