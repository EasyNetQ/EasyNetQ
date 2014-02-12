using System.Collections;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConnectionFactory
    {
        IConnection CreateConnection();
        IConnectionConfiguration Configuration { get; }
        IHostConfiguration CurrentHost { get; }
        bool Next();
        void Success();
        void Reset();
        bool Succeeded { get; }
    }

    public class ConnectionFactoryWrapper : IConnectionFactory
    {
        public virtual IConnectionConfiguration Configuration { get; private set; }
        private readonly IClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterHostSelectionStrategy;

        public ConnectionFactoryWrapper(IConnectionConfiguration connectionConfiguration, IClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterHostSelectionStrategy)
        {
            this.clusterHostSelectionStrategy = clusterHostSelectionStrategy;

            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckAny(connectionConfiguration.Hosts, "connectionConfiguration", "At least one host must be defined in connectionConfiguration");

            Configuration = connectionConfiguration;

            foreach (var hostConfiguration in Configuration.Hosts)
            {
                var connectionFactory = new ConnectionFactory();
                if (connectionConfiguration.AMQPConnectionString != null)
                {
                    connectionFactory.uri = connectionConfiguration.AMQPConnectionString;
                }

                connectionFactory.HostName = hostConfiguration.Host;
                
                if(connectionFactory.VirtualHost == "/")
                    connectionFactory.VirtualHost = Configuration.VirtualHost;
                
                if(connectionFactory.UserName == "guest")
                    connectionFactory.UserName = Configuration.UserName;

                if(connectionFactory.Password == "guest")
                    connectionFactory.Password = Configuration.Password;

                if (connectionFactory.Port == -1)
                    connectionFactory.Port = hostConfiguration.Port;

                if (Configuration.Ssl.Enabled)
                    connectionFactory.Ssl = Configuration.Ssl;

                connectionFactory.RequestedHeartbeat = Configuration.RequestedHeartbeat;
                connectionFactory.ClientProperties = Configuration.ClientProperties;
                clusterHostSelectionStrategy.Add(new ConnectionFactoryInfo(connectionFactory, hostConfiguration));
            }
        }

        private static IDictionary ConvertToHashtable(IDictionary<string, string> clientProperties)
        {
            var dictionary = new Hashtable();
            foreach (var clientProperty in clientProperties)
            {
                dictionary.Add(clientProperty.Key, clientProperty.Value);
            }
            return dictionary;
        }

        public virtual IConnection CreateConnection()
        {
            return clusterHostSelectionStrategy.Current().ConnectionFactory.CreateConnection();
        }

        public virtual IHostConfiguration CurrentHost
        {
            get { return clusterHostSelectionStrategy.Current().HostConfiguration; }
        }

        public virtual bool Next()
        {
            return clusterHostSelectionStrategy.Next();
        }

        public virtual void Reset()
        {
            clusterHostSelectionStrategy.Reset();
        }

        public virtual void Success()
        {
            clusterHostSelectionStrategy.Success();
        }

        public virtual bool Succeeded
        {
            get { return clusterHostSelectionStrategy.Succeeded; }
        }
    }

    public class ConnectionFactoryInfo
    {
        public ConnectionFactoryInfo(ConnectionFactory connectionFactory, IHostConfiguration hostConfiguration)
        {
            ConnectionFactory = connectionFactory;
            HostConfiguration = hostConfiguration;
        }

        public ConnectionFactory ConnectionFactory { get; private set; }
        public IHostConfiguration HostConfiguration { get; private set; }
    }

}