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
        private const string VirtualHost = "123";
        private readonly Uri amqpConnectionString = new Uri(string.Format("amqp://{0}:{1}@localhost:10000/{2}", UserName, Password, VHost));
        private readonly Uri amqpSecureConnectionString = new Uri("amqps://localhost:5671");
        private RandomClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterSelectionStrategy;

        [SetUp]
        public void SetUp()
        {
            clusterSelectionStrategy = new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>();
        }

        [Test]
        public void Should_set_Port_And_HostName_When_Specified()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            ConnectionFactory connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.VirtualHost.ShouldEqual(VHost);
            connectionFactory.Endpoint.Port.ShouldEqual(amqpConnectionString.Port);
            connectionFactory.Endpoint.HostName.ShouldEqual(amqpConnectionString.Host);
        }

        [Test]
        public void Should_set_Post_as_in_amqp_When_Specified_in_config()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            ConnectionFactory connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.Endpoint.Port.ShouldEqual(amqpConnectionString.Port);
        }

        [Test]
        public void Should_set_AMQP_UserName_And_Password_When_Specified()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            ConnectionFactory connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.UserName.ShouldEqual(UserName);
            connectionFactory.Password.ShouldEqual(Password);
        }

        [Test]
        public void Should_set_SslOption()
        {
            InitConnectionFactoryWrapper(amqpSecureConnectionString);

            AmqpTcpEndpoint amqpTcpEndpoint = clusterSelectionStrategy.Current().ConnectionFactory.Endpoint;
            amqpTcpEndpoint.Ssl.Enabled.ShouldBeTrue("ssl not enabled");
            amqpTcpEndpoint.Ssl.AcceptablePolicyErrors.ShouldEqual(SslPolicyErrors.RemoteCertificateNameMismatch);
        }

        [Test]
        public void Should_preserve_VirtualHost_if_specified_by_amqp()
        {
            var vhost = "12345";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://host/{0}", vhost)));

            clusterSelectionStrategy.Current().ConnectionFactory.VirtualHost.ShouldEqual(vhost);
        }
        
        [Test]
        public void Should_preserve_UserName_if_specified_by_amqp()
        {
            var userbla = "userDelta";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://{0}@host", userbla)));

            clusterSelectionStrategy.Current().ConnectionFactory.UserName.ShouldEqual(userbla);
        }

        [Test]
        public void Should_preserve_Password_if_specified_by_amqp()
        {
            var pass = "passDelta";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://user:{0}@host", pass)));

            clusterSelectionStrategy.Current().ConnectionFactory.Password.ShouldEqual(pass);
        }

        [Test]
        public void Should_preserve_Port_if_specified_by_amqp()
        {
            var port = "17325";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://user:pass@host:{0}", port)));

            clusterSelectionStrategy.Current().ConnectionFactory.Port.ShouldEqual(int.Parse(port));
        }

        private void InitConnectionFactoryWrapper(Uri connectionString)
        {
            var connectionConfiguration1 = new ConnectionConfiguration
                {
                    AMQPConnectionString = connectionString,
                    VirtualHost = VirtualHost
                };
            connectionConfiguration1.Validate();
            var connectionConfiguration = connectionConfiguration1;
            new ConnectionFactoryWrapper(connectionConfiguration,clusterSelectionStrategy);
        }
    }
}