// ReSharper disable InconsistentNaming

using System.Linq;
using EasyNetQ.ConnectionString;
using NUnit.Framework;

namespace EasyNetQ.Tests
{
    [TestFixture]
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
    }
}

// ReSharper restore InconsistentNaming