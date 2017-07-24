// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.ConnectionString;
using Xunit;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class ConnectionConfigurationTests
    {
        
        [Fact]
        public void The_validate_method_should_apply_AMQPconnection_idempotently()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://amqphost:1234/");
            connectionConfiguration.Validate(); // Simulates additional call to .Validate(); made by some RabbitHutch.CreateBus(...) overloads, in addition to call within ConnectionStringParser.Parse().  

            connectionConfiguration.Hosts.Count().ShouldEqual(1);
            connectionConfiguration.Hosts.Single().Host.ShouldEqual("amqphost");
            connectionConfiguration.Hosts.Single().Port.ShouldEqual((ushort)1234);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpsPort_Correctly()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqps://user:pass@host/vhost");
            connectionConfiguration.Validate(); 

            connectionConfiguration.Port.ShouldEqual((ushort)5671);
        }

        [Fact]
        public void The_validate_method_should_apply_Default_AmqpPort_Correctly()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.Validate();

            connectionConfiguration.Port.ShouldEqual((ushort)5672);
        }

        [Fact]
        public void The_validate_method_should_apply_NonDefault_VirtualHost_Correctly()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/vhost");
            connectionConfiguration.Validate();

            connectionConfiguration.VirtualHost.ShouldEqual("vhost");
        }

        [Fact]
        public void The_validate_method_should_apply_Default_VirtualHost_Correctly()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://user:pass@host/");
            connectionConfiguration.Validate();

            connectionConfiguration.VirtualHost.ShouldEqual("/");
        }

        [Fact]
        public void The_AuthMechanisms_property_should_default_to_PlainMechanism()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionConfiguration();

            connectionConfiguration.AuthMechanisms.Count.ShouldEqual(1);
            connectionConfiguration.AuthMechanisms.Single().ShouldBeOfType<PlainMechanismFactory>();
        }
    }
}

// ReSharper restore InconsistentNaming