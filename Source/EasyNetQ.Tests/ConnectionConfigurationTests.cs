// ReSharper disable InconsistentNaming

using EasyNetQ.ConnectionString;
using FluentAssertions;
using RabbitMQ.Client;
using System.Linq;
using Xunit;

namespace EasyNetQ.Tests
{
    public class ConnectionConfigurationTests
    {
        [Fact]
        public void The_validate_method_should_apply_AMQPconnection_idempotently()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://amqphost:1234/");
            connectionConfiguration.SetDefaultProperties();
            connectionConfiguration.Hosts.Count().Should().Be(1);
            connectionConfiguration.Hosts.Single().Host.Should().Be("amqphost");
            connectionConfiguration.Hosts.Single().Port.Should().Be(1234);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpsPort_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqps://user:pass@host/vhost");
            connectionConfiguration.SetDefaultProperties();

            connectionConfiguration.Port.Should().Be(5671);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpPort_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.SetDefaultProperties();

            connectionConfiguration.Port.Should().Be(5672);
        }

        [Fact]
        public void The_validate_method_should_apply_NonDefault_VirtualHost_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.SetDefaultProperties();

            connectionConfiguration.VirtualHost.Should().Be("vhost");
        }

        [Fact]
        public void The_validate_method_should_apply_Default_VirtualHost_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/");
            connectionConfiguration.SetDefaultProperties();

            connectionConfiguration.VirtualHost.Should().Be("/");
        }

        [Fact]
        public void The_AuthMechanisms_property_should_default_to_PlainMechanism()
        {
            var connectionConfiguration = new ConnectionConfiguration();

            connectionConfiguration.AuthMechanisms.Count.Should().Be(1);
            connectionConfiguration.AuthMechanisms.Single().Should().BeOfType<PlainMechanismFactory>();
        }
    }
}

// ReSharper restore InconsistentNaming
