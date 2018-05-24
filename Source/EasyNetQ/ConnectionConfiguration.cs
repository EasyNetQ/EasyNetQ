using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class ConnectionConfiguration
    {
        private const int DefaultPort = 5672;
        private const int DefaultAmqpsPort = 5671;
        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>
        /// Heartbeat interval seconds. (default is 10)
        /// </summary>
        public ushort RequestedHeartbeat { get; set; }
        public ushort PrefetchCount { get; set; }
        public Uri AMQPConnectionString { get; set; }
        public IDictionary<string, object> ClientProperties { get; } 

        public IEnumerable<HostConfiguration> Hosts { get; set; }
        public SslOption Ssl { get; }
        /// <summary>
        /// Operation timeout seconds. (default is 10)
        /// </summary>
        public ushort Timeout { get; set; }
        public bool PublisherConfirms { get; set; }
        public bool PersistentMessages { get; set; }
        public string Product { get; set; }
        public string Platform { get; set; }
        public string Name { get; set; }
        public bool UseBackgroundThreads { get; set; }
        public IList<AuthMechanismFactory> AuthMechanisms { get; set; }
        public TimeSpan ConnectIntervalAttempt { get;  set; }

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
            UseBackgroundThreads = false;
            ConnectIntervalAttempt = TimeSpan.FromSeconds(5);
                         
            // prefetchCount determines how many messages will be allowed in the local in-memory queue
            // setting to zero makes this infinite, but risks an out-of-memory exception.
            // set to 50 based on this blog post:
            // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
            PrefetchCount = 50;
            AuthMechanisms = new AuthMechanismFactory[] {new PlainMechanismFactory()};
            
            Hosts = new List<HostConfiguration>();

            Ssl = new SslOption();
            ClientProperties = new Dictionary<string, object>();
        }

        private void SetDefaultClientProperties(IDictionary<string, object> clientProperties)
        {
            string applicationNameAndPath = null;
#if !NETFX
            var version = this.GetType().GetTypeInfo().Assembly.GetName().Version.ToString();
#else
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
#endif
            applicationNameAndPath = Environment.GetCommandLineArgs()[0];

            var applicationName = "unknown";
            var applicationPath = "unknown";
            if (!string.IsNullOrWhiteSpace(applicationNameAndPath))
            {
                // Note: When running the application in an Integration Services Package (SSIS) the
                // Environment.GetCommandLineArgs()[0] can return null, and therefor it is not possible to get
                // the filename or directory name.
                try
                {
                    // Will only throw an exception if the applicationName contains invalid characters, is empty, or too long
                    // Silently catch the exception, as we will just leave the application name and path to "unknown"
                    applicationName = Path.GetFileName(applicationNameAndPath);
                    applicationPath = Path.GetDirectoryName(applicationNameAndPath);
                }
                catch (ArgumentException) { }
                catch (PathTooLongException) { }
            }

            var hostname = Environment.MachineName;

            var product = Product ?? applicationName;
            var platform = Platform ?? hostname;
            var name = Name ?? applicationName;

            AddValueIfNotExists(clientProperties, "client_api", "EasyNetQ");
            AddValueIfNotExists(clientProperties, "product", product);
            AddValueIfNotExists(clientProperties, "platform", platform);
            AddValueIfNotExists(clientProperties, "version", version);
            AddValueIfNotExists(clientProperties, "connection_name", name);
            AddValueIfNotExists(clientProperties, "easynetq_version", version);
            AddValueIfNotExists(clientProperties, "application", applicationName);
            AddValueIfNotExists(clientProperties, "application_location", applicationPath);
            AddValueIfNotExists(clientProperties, "machine_name", hostname);
            AddValueIfNotExists(clientProperties, "user", UserName);
            AddValueIfNotExists(clientProperties, "connected", DateTime.UtcNow.ToString("u")); // UniversalSortableDateTimePattern: yyyy'-'MM'-'dd HH':'mm':'ss'Z'
            AddValueIfNotExists(clientProperties, "requested_heartbeat", RequestedHeartbeat.ToString());
            AddValueIfNotExists(clientProperties, "timeout", Timeout.ToString());
            AddValueIfNotExists(clientProperties, "publisher_confirms", PublisherConfirms.ToString());
            AddValueIfNotExists(clientProperties, "persistent_messages", PersistentMessages.ToString());
        }

        private static void AddValueIfNotExists(IDictionary<string, object> clientProperties, string name, string value)
        {
            if (!clientProperties.ContainsKey(name))
                clientProperties.Add(name, value);
        }

        public void Validate()
        {
            if (AMQPConnectionString != null && !Hosts.Any(h => h.Host == AMQPConnectionString.Host))
            {
                if (Port == DefaultPort)
                {
                    if (AMQPConnectionString.Port > 0)
                        Port = (ushort)AMQPConnectionString.Port;
                    else if(AMQPConnectionString.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
                        Port = DefaultAmqpsPort;
                }
                if (AMQPConnectionString.Segments.Length > 1)
                {
                    VirtualHost = AMQPConnectionString.Segments.Last();
                }
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
                    hostConfiguration.Port = Port;
                }
            }

            SetDefaultClientProperties(ClientProperties);
        }
    }

    public class HostConfiguration
    {
        public HostConfiguration()
        {
            Ssl = new SslOption();
        }

        public string Host { get; set; }
        public ushort Port { get; set; }
        public SslOption Ssl { get; }
    }
}