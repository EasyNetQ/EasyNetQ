﻿// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyNetQ.ConnectionString;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EasyNetQ.Tests
{
    public class ConnectionConfigurationTests
    {
        
        [Test]
        public void The_validate_method_should_apply_AMQPconnection_idempotently()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionStringParser().Parse("amqp://amqphost:1234/");
            connectionConfiguration.Validate(); // Simulates additional call to .Validate(); made by some RabbitHutch.CreateBus(...) overloads, in addition to call within ConnectionStringParser.Parse().  

            connectionConfiguration.Hosts.Count().ShouldEqual(1);
            connectionConfiguration.Hosts.Single().Host.ShouldEqual("amqphost");
            connectionConfiguration.Hosts.Single().Port.ShouldEqual(1234);
        }

        [Test]
        public void The_AuthMechanisms_property_should_default_to_PlainMechanism()
        {
            ConnectionConfiguration connectionConfiguration = new ConnectionConfiguration();

            connectionConfiguration.AuthMechanisms.Count.ShouldEqual(1);
            connectionConfiguration.AuthMechanisms.Single().ShouldBeOfType<PlainMechanismFactory>();
        }
    }
}

// ReSharper restore InconsistentNaming