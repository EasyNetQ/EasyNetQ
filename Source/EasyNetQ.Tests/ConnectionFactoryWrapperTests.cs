using System;
using System.Net.Security;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class ConnectionFactoryWrapperTests
    {
        private const string VHost = "vhost";
        private const string UserName = "user";
        private const string Password = "pass";
        private const string VirtualHost = "123";
        private readonly Uri amqpConnectionString = new Uri(string.Format("amqp://{0}:{1}@localhost:10000/{2}", UserName, Password, VHost));
        private readonly Uri amqpSecureConnectionString = new Uri("amqps://localhost:5671");
        private RandomClusterHostSelectionStrategy<ConnectionFactoryInfo> clusterSelectionStrategy;

        public ConnectionFactoryWrapperTests()
        {
            clusterSelectionStrategy = new RandomClusterHostSelectionStrategy<ConnectionFactoryInfo>();
        }

        [Fact]
        public void Should_set_Port_And_HostName_When_Specified()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            var connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.VirtualHost.Should().Be(VHost);
            connectionFactory.Endpoint.Port.Should().Be(amqpConnectionString.Port);
            connectionFactory.Endpoint.HostName.Should().Be(amqpConnectionString.Host);
        }

        [Fact]
        public void Should_set_Post_as_in_amqp_When_Specified_in_config()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            var connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.Endpoint.Port.Should().Be(amqpConnectionString.Port);
        }

        [Fact]
        public void Should_set_AMQP_UserName_And_Password_When_Specified()
        {
            InitConnectionFactoryWrapper(amqpConnectionString);

            var connectionFactory = clusterSelectionStrategy.Current().ConnectionFactory;
            connectionFactory.UserName.Should().Be(UserName);
            connectionFactory.Password.Should().Be(Password);
        }

        [Fact]
        public void Should_set_SslOption()
        {
            InitConnectionFactoryWrapper(amqpSecureConnectionString);

            var amqpTcpEndpoint = clusterSelectionStrategy.Current().ConnectionFactory.Endpoint;
            amqpTcpEndpoint.Ssl.Enabled.Should().BeTrue("ssl not enabled");
            amqpTcpEndpoint.Ssl.AcceptablePolicyErrors.Should().Be(SslPolicyErrors.RemoteCertificateNameMismatch);
        }

        [Fact]
        public void Should_preserve_VirtualHost_if_specified_by_amqp()
        {
            var vhost = "12345";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://host/{0}", vhost)));

            clusterSelectionStrategy.Current().ConnectionFactory.VirtualHost.Should().Be(vhost);
        }
        
        [Fact]
        public void Should_preserve_UserName_if_specified_by_amqp()
        {
            var userbla = "userDelta";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://{0}@host", userbla)));

            clusterSelectionStrategy.Current().ConnectionFactory.UserName.Should().Be(userbla);
        }

        [Fact]
        public void Should_preserve_Password_if_specified_by_amqp()
        {
            var pass = "passDelta";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://user:{0}@host", pass)));

            clusterSelectionStrategy.Current().ConnectionFactory.Password.Should().Be(pass);
        }

        [Fact]
        public void Should_preserve_Port_if_specified_by_amqp()
        {
            var port = "17325";
            InitConnectionFactoryWrapper(new Uri(string.Format("amqp://user:pass@host:{0}", port)));

            clusterSelectionStrategy.Current().ConnectionFactory.Port.Should().Be(int.Parse(port));
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