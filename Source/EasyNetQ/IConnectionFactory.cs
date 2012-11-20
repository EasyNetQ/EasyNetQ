using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
            if(connectionConfiguration == null)
            {
                throw new ArgumentNullException("connectionConfiguration");
            }
            if (!connectionConfiguration.Hosts.Any())
            {
                throw new EasyNetQException("At least one host must be defined in connectionConfiguration");
            }

            Configuration = connectionConfiguration;

            foreach (var hostConfiguration in Configuration.Hosts)
            {
                clusterHostSelectionStrategy.Add(new ConnectionFactoryInfo(new ConnectionFactory
                    {
                        HostName = hostConfiguration.Host,
                        Port = hostConfiguration.Port,
                        VirtualHost = Configuration.VirtualHost,
                        UserName = Configuration.UserName,
                        Password = Configuration.Password,
                        ClientProperties = ConvertToHashtable(Configuration.ClientProperties)
                    }, hostConfiguration));
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