using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public class ConnectionConfiguration
    {
        private const int DefaultPort = 5672;
        private const int DefaultAmqpsPort = 5671;

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
            DispatcherQueueSize = 1024;

            // prefetchCount determines how many messages will be allowed in the local in-memory queue
            // setting to zero makes this infinite, but risks an out-of-memory exception.
            // set to 50 based on this blog post:
            // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
            PrefetchCount = 50;
            AuthMechanisms = new IAuthMechanismFactory[] { new PlainMechanismFactory() };

            Hosts = new List<HostConfiguration>();

            Ssl = new SslOption();
            ClientProperties = new Dictionary<string, object>();
        }

        /// <summary>
        /// Dictionary of client properties to be sent to the server.
        /// You can browse these properties when selecting connection in RabbitMQ Management Plugin.
        /// All properties with <c>null</c> values will be displayed as 'undefined'.
        /// </summary>
        public IDictionary<string, object> ClientProperties { get; }

        public ushort Port { get; set; }

        public string VirtualHost { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public string UserName { get; set; }

        public string Password { get; set; }

        /// <summary>
        /// Heartbeat interval seconds. (default is 10)
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public ushort RequestedHeartbeat { get; set; }

        public ushort PrefetchCount { get; set; }

        public Uri AmqpConnectionString { get; set; }

        public IEnumerable<HostConfiguration> Hosts { get; set; }

        public SslOption Ssl { get; }

        /// <summary>
        /// Operation timeout seconds. (default is 10)
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public ushort Timeout { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public bool PublisherConfirms { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public bool PersistentMessages { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public string Product { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// This property will be written to <see cref="ClientProperties"/> if it is not there yet.
        /// </summary>
        public string Name { get; set; }

        public bool UseBackgroundThreads { get; set; }

        public IList<IAuthMechanismFactory> AuthMechanisms { get; set; }

        public TimeSpan ConnectIntervalAttempt { get; set; }

        public int DispatcherQueueSize { get; set; }

        private void SetDefaultClientProperties(IDictionary<string, object> clientProperties)
        {
            var applicationNameAndPath = Environment.GetCommandLineArgs()[0];

            var applicationName = "unknown";
            var applicationPath = "unknown";
            if (!string.IsNullOrWhiteSpace(applicationNameAndPath))
                try
                {
                    // Will only throw an exception if the applicationName contains invalid characters, is empty, or too long
                    // Silently catch the exception, as we will just leave the application name and path to "unknown"
                    applicationName = Path.GetFileName(applicationNameAndPath);
                    applicationPath = Path.GetDirectoryName(applicationNameAndPath);
                }
                catch (ArgumentException)
                {
                }
                catch (PathTooLongException)
                {
                }

            AddValueIfNotExists(clientProperties, "client_api", "EasyNetQ");
            AddValueIfNotExists(clientProperties, "product", Product ?? applicationName);
            AddValueIfNotExists(clientProperties, "platform", Platform ?? GetPlatform());
            AddValueIfNotExists(clientProperties, "os", Environment.OSVersion.ToString());
            AddValueIfNotExists(clientProperties, "version", GetApplicationVersion());
            AddValueIfNotExists(clientProperties, "connection_name", Name ?? applicationName);
            AddValueIfNotExists(clientProperties, "easynetq_version", typeof(ConnectionConfiguration).Assembly.GetName().Version.ToString());
            AddValueIfNotExists(clientProperties, "application", applicationName);
            AddValueIfNotExists(clientProperties, "application_location", applicationPath);
            AddValueIfNotExists(clientProperties, "machine_name", Environment.MachineName);
            AddValueIfNotExists(clientProperties, "user", UserName);
            AddValueIfNotExists(clientProperties, "connected", DateTime.UtcNow.ToString("u")); // UniversalSortableDateTimePattern: yyyy'-'MM'-'dd HH':'mm':'ss'Z'
            AddValueIfNotExists(clientProperties, "requested_heartbeat", RequestedHeartbeat.ToString());
            AddValueIfNotExists(clientProperties, "timeout", Timeout.ToString());
            AddValueIfNotExists(clientProperties, "publisher_confirms", PublisherConfirms.ToString());
            AddValueIfNotExists(clientProperties, "persistent_messages", PersistentMessages.ToString());
        }

        private static void AddValueIfNotExists(IDictionary<string, object> clientProperties, string name, string value)
        {
            // allow set nulls, null values will be displayed as 'undefined'
            if (!clientProperties.ContainsKey(name))
                clientProperties.Add(name, value);
        }

        public void Validate()
        {
            if (AmqpConnectionString != null && Hosts.All(h => h.Host != AmqpConnectionString.Host))
            {
                if (Port == DefaultPort)
                {
                    if (AmqpConnectionString.Port > 0)
                        Port = (ushort)AmqpConnectionString.Port;
                    else if (AmqpConnectionString.Scheme.Equals("amqps", StringComparison.OrdinalIgnoreCase))
                        Port = DefaultAmqpsPort;
                }

                if (AmqpConnectionString.Segments.Length > 1) VirtualHost = AmqpConnectionString.Segments.Last();

                Hosts = Hosts.Concat(new[] { new HostConfiguration { Host = AmqpConnectionString.Host } });
            }

            if (!Hosts.Any())
                throw new EasyNetQException("Invalid connection string. 'host' value must be supplied. e.g: \"host=myserver\"");

            foreach (var hostConfiguration in Hosts)
                if (hostConfiguration.Port == 0)
                    hostConfiguration.Port = Port;

            SetDefaultClientProperties(ClientProperties);
        }

        private static string GetApplicationVersion()
        {
            try
            {
                return Assembly.GetEntryAssembly()?.GetName().Version.ToString();
            }
            catch
            {
                return null;
            }
        }

        private static string GetPlatform()
        {
#if NETSTANDARD2_0
            string platform = RuntimeInformation.FrameworkDescription;
#else
            string platform = Environment.Version.ToString();
#endif

            string frameworkName = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
            if (frameworkName != null)
                platform = platform + " [" + frameworkName + "]";

            // example: .NET Core 4.6.27317.07 [.NETCoreApp,Version=v2.0]
            return platform;
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
