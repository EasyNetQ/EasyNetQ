using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace EasyNetQ
{
    public interface IConnectionConfiguration
    {
        ushort Port { get; }
        string VirtualHost { get; }
        string UserName { get; }
        string Password { get; }
        ushort RequestedHeartbeat { get; }
        ushort PrefetchCount { get; }
        IDictionary<string, string> ClientProperties { get; } 
        
        IEnumerable<IHostConfiguration> Hosts { get; }
    }

    public interface IHostConfiguration
    {
        string Host { get; }
        ushort Port { get; }
    }

    public class ConnectionConfiguration : IConnectionConfiguration
    {
        public ushort Port { get; set; }
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }
        public ushort PrefetchCount { get; set; }
        public IDictionary<string, string> ClientProperties { get; private set; } 

        public IEnumerable<IHostConfiguration> Hosts { get; set; }


        public ConnectionConfiguration()
        {
            // set default values
            Port = 5672;
            VirtualHost = "/";
            UserName = "guest";
            Password = "guest";
            RequestedHeartbeat = 0;

            // prefetchCount determines how many messages will be allowed in the local in-memory queue
            // setting to zero makes this infinite, but risks an out-of-memory exception.
            // set to 50 based on this blog post:
            // http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
            PrefetchCount = 50;
            
            Hosts = new List<IHostConfiguration>();
            ClientProperties = new Dictionary<string, string>();
            SetDefaultClientProperties(ClientProperties);
        }

        private void SetDefaultClientProperties(IDictionary<string, string> clientProperties)
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            var applicationNameAndPath = Environment.GetCommandLineArgs()[0];
            var applicationName = Path.GetFileName(applicationNameAndPath);
            var applicationPath = Path.GetDirectoryName(applicationNameAndPath);
            var hostname = Environment.MachineName;

            clientProperties.Add("Client API", "EasyNetQ");
            clientProperties.Add("EasyNetQ Version", version);
            clientProperties.Add("Application", applicationName);
            clientProperties.Add("Application Location", applicationPath);
            clientProperties.Add("Machine Name", hostname);
            clientProperties.Add("User", UserName);
            clientProperties.Add("Connected", DateTime.Now.ToString("MM/dd/yy HH:mm:ss"));

        }

        public void Validate()
        {
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
        }
    }

    public class HostConfiguration : IHostConfiguration
    {
        public string Host { get; set; }
        public ushort Port { get; set; }
    }
}