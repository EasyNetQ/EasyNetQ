// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.ConnectionString;
using FluentAssertions;
using Xunit;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class ConnectionConfigurationTests
    {
        
        [Fact]
        public void The_validate_method_should_apply_AMQPconnection_idempotently()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://amqphost:1234/");
            connectionConfiguration.Validate(); // Simulates additional call to .Validate(); made by some RabbitHutch.CreateBus(...) overloads, in addition to call within ConnectionStringParser.Parse().  

            connectionConfiguration.Hosts.Count().Should().Be(1);
            connectionConfiguration.Hosts.Single().Host.Should().Be("amqphost");
            connectionConfiguration.Hosts.Single().Port.Should().Be(1234);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpsPort_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqps://user:pass@host/vhost");
            connectionConfiguration.Validate();

            connectionConfiguration.Port.Should().Be(5671);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpPort_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.Validate();

            connectionConfiguration.Port.Should().Be(5672);
        }

        [Fact]
        public void The_validate_method_should_apply_NonDefault_VirtualHost_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.Validate();

            connectionConfiguration.VirtualHost.Should().Be("vhost");
        }

        [Fact]
        public void The_validate_method_should_apply_Default_VirtualHost_Correctly()
        {
            var connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/");
            connectionConfiguration.Validate();

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