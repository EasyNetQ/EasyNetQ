using System.Collections;
using System.Collections.Generic;
using RabbitMQ.Client;

namespace EasyNetQ
{
    public interface IConnectionFactory
    {
        IConnection CreateConnection();
        ConnectionConfiguration Configuration { get; }
        HostConfiguration CurrentHost { get; }
        bool Next();
        void Success();
        void Reset();
        bool Succeeded { get; }
    }

    public class ConnectionFactoryWrapper : IConnectionFactory
    {
        public virtual ConnectionConfiguration Configuration { get; private set; }
        private readonly IClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterHostSelectionStrategy;

        public ConnectionFactoryWrapper(ConnectionConfiguration connectionConfiguration, IClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterHostSelectionStrategy)
        {
            this.clusterHostSelectionStrategy = clusterHostSelectionStrategy;

            Preconditions.CheckNotNull(connectionConfiguration, "connectionConfiguration");
            Preconditions.CheckAny(connectionConfiguration.Hosts, "connectionConfiguration", "At least one host must be defined in connectionConfiguration");

            Configuration = connectionConfiguration;

            foreach (var hostConfiguration in Configuration.Hosts)
            {
                var connectionFactory = new ConnectionFactory
                {
                    UseBackgroundThreadsForIO = false,
                    AutomaticRecoveryEnabled = false,
                    TopologyRecoveryEnabled = false
                };

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

        public virtual IConnection CreateConnection()
        {
            return clusterHostSelectionStrategy.Current().ConnectionFactory.CreateConnection();
        }

        public virtual HostConfiguration CurrentHost
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
        public ConnectionFactoryInfo(ConnectionFactory connectionFactory, HostConfiguration hostConfiguration)
        {
            ConnectionFactory = connectionFactory;
            HostConfiguration = hostConfiguration;
        }

        public ConnectionFactory ConnectionFactory { get; private set; }
        public HostConfiguration HostConfiguration { get; private set; }
    }

}