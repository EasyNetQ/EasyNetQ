using System;
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
        public IConnectionConfiguration Configuration { get; private set; }
        private readonly TryNextCollection<ConnectionFactoryInfo> connectionFactores = new TryNextCollection<ConnectionFactoryInfo>();

        public ConnectionFactoryWrapper(IConnectionConfiguration connectionConfiguration)
        {
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
                connectionFactores.Add(new ConnectionFactoryInfo(new ConnectionFactory
                    {
                        HostName = hostConfiguration.Host,
                        Port = hostConfiguration.Port,
                        VirtualHost = Configuration.VirtualHost,
                        UserName = Configuration.UserName,
                        Password = Configuration.Password
                    }, hostConfiguration));
            }
        }

        public virtual IConnection CreateConnection()
        {
            return connectionFactores.Current().ConnectionFactory.CreateConnection();
        }

        public IHostConfiguration CurrentHost
        {
            get { return connectionFactores.Current().HostConfiguration; }
        }

        public bool Next()
        {
            return connectionFactores.Next();
        }

        public void Reset()
        {
            connectionFactores.Reset();
        }

        public void Success()
        {
            connectionFactores.Success();
        }

        public bool Succeeded
        {
            get { return connectionFactores.Succeeded; }
        }

        class ConnectionFactoryInfo
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
}