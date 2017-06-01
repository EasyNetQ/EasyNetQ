// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.ConnectionString;
using Xunit;

namespace EasyNetQ.Tests.ConnectionString
{
    public class ConnectionStringParserTests
    {
        private ConnectionStringParser connectionStringParser;

        private const string connectionString =
            "virtualHost=Copa;username=Copa;host=192.168.1.1;password=abc_xyz;port=12345;" +
            "requestedHeartbeat=3;prefetchcount=2;timeout=12;publisherConfirms=true;cancelOnHaFailover=true;" +
            "useBackgroundThreads=true";

        public ConnectionStringParserTests()
        {
            connectionStringParser = new ConnectionStringParser();
        }

        [Fact]
        public void Should_correctly_parse_connection_string()
        {
            var connectionConfiguration = connectionStringParser.Parse(connectionString);

            connectionConfiguration.Hosts.First().Host.ShouldEqual("192.168.1.1");
            connectionConfiguration.VirtualHost.ShouldEqual("Copa");
            connectionConfiguration.UserName.ShouldEqual("Copa");
            connectionConfiguration.Password.ShouldEqual("abc_xyz");
            connectionConfiguration.Port.ShouldEqual((ushort)12345);
            connectionConfiguration.RequestedHeartbeat.ShouldEqual((ushort)3);
            connectionConfiguration.PrefetchCount.ShouldEqual((ushort)2);
            connectionConfiguration.Timeout.ShouldEqual((ushort)12);
            connectionConfiguration.PublisherConfirms.ShouldBeTrue();
            connectionConfiguration.CancelOnHaFailover.ShouldBeTrue();
            connectionConfiguration.UseBackgroundThreads.ShouldBeTrue();
        }

        [Fact]
        public void Should_parse_global_timeout()
        {
            const string connectionStringWithTimeout = "host=localhost;timeout=13";
            var connectionConfiguration = connectionStringParser.Parse(connectionStringWithTimeout);

            connectionConfiguration.Timeout.ShouldEqual((ushort)13);
        }

        [Fact]
        public void Should_parse_global_persistentMessages()
        {
            const string connectionStringWithPersistenMessages = "host=localhost;persistentMessages=false";
            var connectionConfiguration = connectionStringParser.Parse(connectionStringWithPersistenMessages);

            connectionConfiguration.PersistentMessages.ShouldBeFalse();
        }

        [Fact]
        public void Should_Throw_Exception_OnInvalidAmqp()
        {
            Assert.Throws<EasyNetQException>(() => connectionStringParser.Parse("amqp=Foo"));
        }

        [Fact]
        public void Should_throw_exception_for_unknown_key_at_the_beginning()
        {
            Assert.Throws<EasyNetQException>(() => connectionStringParser.Parse("unknownKey=true"));
        }

        [Fact]
        public void Should_throw_exception_for_unknown_key_at_the_end()
        {
            Assert.Throws<EasyNetQException>(() => connectionStringParser.Parse("host=localhost;unknownKey=true"));
        }

        [Theory]
        [MemberData("AppendixAExamples")]
        public void Should_parse_Examples(AmqpSpecification spec)
        {
            ConnectionConfiguration connectionConfiguration = connectionStringParser.Parse("" + spec.amqpUri);

            connectionConfiguration.Port.ShouldEqual((ushort)spec.port);
            connectionConfiguration.AMQPConnectionString.ShouldEqual(spec.amqpUri);
            connectionConfiguration.Hosts.First().Host.ShouldEqual(spec.host);
            connectionConfiguration.Hosts.First().Port.ShouldEqual((ushort)spec.port);
        }

// ReSharper disable UnusedMethodReturnValue.Local
        private static IEnumerable<object[]> AppendixAExamples()
// ReSharper restore UnusedMethodReturnValue.Local
        {
            yield return new[] { new AmqpSpecification(new Uri("amqp://user:pass@host:10000/vhost"), "host", 10000) };
            yield return new[] { new AmqpSpecification(new Uri("amqp://"), "", 5672) };
            yield return new[] { new AmqpSpecification(new Uri("amqp://host"), "host", 5672) };
            yield return new[] { new AmqpSpecification(new Uri("amqps://host"), "host", 5671) };
        }

        [Fact]
        public void Should_UsePort_From_ConnectionString()
        {
            ConnectionConfiguration connectionConfiguration = connectionStringParser.Parse("amqp=amqp://host/;port=123");

            connectionConfiguration.Port.ShouldEqual((ushort)123);
        }

        [Fact]
        public void Should_NotUsePort_From_ConnectionString()
        {
            ConnectionConfiguration connectionConfiguration = connectionStringParser.Parse("amqp=amqp://host:1234/");

            connectionConfiguration.Port.ShouldEqual((ushort)1234);
        }

        [Fact]
        public void Should_AddHost_ToHosts()
        {
            ConnectionConfiguration connectionConfiguration = connectionStringParser.Parse("host=local;amqp=amqp://amqphost:1234/");

            connectionConfiguration.Hosts.Count().ShouldEqual(2);
            connectionConfiguration.Hosts.First().Host.ShouldEqual("local");
            connectionConfiguration.Hosts.Last().Host.ShouldEqual("amqphost");
        }

        public class AmqpSpecification
        {
            public readonly string host;

            public readonly int port;

            public readonly Uri amqpUri;

            public AmqpSpecification(Uri amqpUri, string host, int port)
            {
                this.host = host;
                this.port = port;
                this.amqpUri = amqpUri;
            }

            public override string ToString()
            {
                return string.Format("AmqpUri: {0}", amqpUri);
            }
        }
    }
}

// ReSharper restore InconsistentNaming