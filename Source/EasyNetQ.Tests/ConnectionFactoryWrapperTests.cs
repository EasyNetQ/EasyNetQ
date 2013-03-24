using System;
using System.Net.Security;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    [TestFixture]
    public class ConnectionFactoryWrapperTests
    {
        private const string VHost = "vhost";
        private const string UserName = "user";
        private const string Password = "pass";
        private readonly Uri amqpConnectionString = new Uri(string.Format("amqp://{0}:{1}@localhost:5671/{2}", UserName, Password, VHost));
        private readonly Uri amqpSecureConnectionString = new Uri("amqps://localhost:5671");
        private DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterSelectionStrategy;

        [SetUp]
        public void SetUp()
        {
            clusterSelectionStrategy = new DefaultClusterHostSelectionStrategy<ConnectionFactoryInfo>();
        }

        [Test]
        public void Should_set_PortAndHostName_When_Specified()
        {
            var connectionConfiguration = new ConnectionConfiguration
                {
                    AMQPConnectionString = amqpConnectionString,
                    Hosts = new[] {new HostConfiguration(),}
                };
            new ConnectionFactoryWrapper(connectionConfiguration,
                                         clusterSelectionStrategy);

            ConnectionFactory connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.VirtualHost.ShouldEqual(VHost);
            AmqpTcpEndpoint amqpTcpEndpoint = connectionFactory.Endpoint;
            amqpTcpEndpoint.Port.ShouldEqual(amqpConnectionString.Port);
            amqpTcpEndpoint.HostName.ShouldEqual(amqpConnectionString.Host);
        }

        [Test]
        public void Should_set_AMQP_UserName_And_Password_When_Specified()
        {
            var connectionConfiguration = new ConnectionConfiguration
            {
                AMQPConnectionString = amqpConnectionString,
                Hosts = new[] { new HostConfiguration(), }
            };
            new ConnectionFactoryWrapper(connectionConfiguration,
                                         clusterSelectionStrategy);

            ConnectionFactory connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.UserName.ShouldEqual(UserName);
            connectionFactory.Password.ShouldEqual(Password);
        }

        [Test]
        public void Should_set_SslOption()
        {
            var connectionConfiguration = new ConnectionConfiguration
            {
                AMQPConnectionString = amqpSecureConnectionString,
                Hosts = new[] { new HostConfiguration(), }
            };
            new ConnectionFactoryWrapper(connectionConfiguration,
                                         clusterSelectionStrategy);

            AmqpTcpEndpoint amqpTcpEndpoint = clusterSelectionStrategy.Current().ConnectionFactory.Endpoint;
            amqpTcpEndpoint.Ssl.Enabled.ShouldBeTrue("ssl not enabled");
            amqpTcpEndpoint.Ssl.AcceptablePolicyErrors.ShouldEqual(SslPolicyErrors.RemoteCertificateNameMismatch);
        }
    }
}