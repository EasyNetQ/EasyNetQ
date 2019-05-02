// ReSharper disable InconsistentNaming

using System;
using System.Collections.Generic;
using System.Linq;
using EasyNetQ.ConnectionString;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.ConnectionString
{
    public class ConnectionStringParserTests
    {
        public ConnectionStringParserTests()
        {
            connectionStringParser = new ConnectionStringParser();
        }

        private readonly ConnectionStringParser connectionStringParser;

        private const string connectionString =
            "virtualHost=Copa;username=Copa;host=192.168.1.1;password=abc_xyz;port=12345;" +
            "requestedHeartbeat=3;prefetchcount=2;timeout=12;publisherConfirms=true;" +
            "useBackgroundThreads=true;" +
            "name=unit-test";

        [Theory]
        [MemberData(nameof(AppendixAExamples))]
        public void Should_parse_Examples(AmqpSpecification spec)
        {
            var connectionConfiguration = connectionStringParser.Parse(spec.amqpUri.ToString());

            connectionConfiguration.Port.Should().Be((ushort) spec.port);
            connectionConfiguration.AMQPConnectionString.Should().Be(spec.amqpUri);
            connectionConfiguration.Hosts.First().Host.Should().Be(spec.host);
            connectionConfiguration.Hosts.First().Port.Should().Be((ushort) spec.port);
            connectionConfiguration.VirtualHost.Should().Be(spec.vhost);
        }

// ReSharper disable UnusedMethodReturnValue.Local
        public static IEnumerable<object[]> AppendixAExamples()
// ReSharper restore UnusedMethodReturnValue.Local
        {
            yield return new[] {new AmqpSpecification(new Uri("amqp://user:pass@host:10000/vhost"), "host", 10000, "vhost")};
            yield return new[] {new AmqpSpecification(new Uri("amqp://"), "", 5672, "/")};
            yield return new[] {new AmqpSpecification(new Uri("amqp://host"), "host", 5672, "/")};
            yield return new[] {new AmqpSpecification(new Uri("amqps://host"), "host", 5671, "/")};
        }

        public class AmqpSpecification
        {
            public readonly Uri amqpUri;
            public readonly string host;

            public readonly int port;

            public readonly string vhost;

            public AmqpSpecification(Uri amqpUri, string host, int port, string vhost)
            {
                this.host = host;
                this.port = port;
                this.vhost = vhost;
                this.amqpUri = amqpUri;
            }

            public override string ToString()
            {
                return string.Format("AmqpUri: {0}", amqpUri);
            }
        }

        [Fact]
        public void Should_AddHost_ToHosts()
        {
            var connectionConfiguration = connectionStringParser.Parse("host=local;amqp=amqp://amqphost:1234/");

            connectionConfiguration.Hosts.Count().Should().Be(2);
            connectionConfiguration.Hosts.First().Host.Should().Be("local");
            connectionConfiguration.Hosts.Last().Host.Should().Be("amqphost");
        }

        [Fact]
        public void Should_correctly_parse_connection_string()
        {
            var connectionConfiguration = connectionStringParser.Parse(connectionString);

            connectionConfiguration.Hosts.First().Host.Should().Be("192.168.1.1");
            connectionConfiguration.VirtualHost.Should().Be("Copa");
            connectionConfiguration.UserName.Should().Be("Copa");
            connectionConfiguration.Password.Should().Be("abc_xyz");
            connectionConfiguration.Port.Should().Be(12345);
            connectionConfiguration.RequestedHeartbeat.Should().Be(3);
            connectionConfiguration.PrefetchCount.Should().Be(2);
            connectionConfiguration.Timeout.Should().Be(12);
            connectionConfiguration.PublisherConfirms.Should().BeTrue();
            connectionConfiguration.UseBackgroundThreads.Should().BeTrue();
            connectionConfiguration.Name.Should().Be("unit-test");
        }

        [Fact]
        public void Should_NotUsePort_From_ConnectionString()
        {
            var connectionConfiguration = connectionStringParser.Parse("amqp=amqp://host:1234/");

            connectionConfiguration.Port.Should().Be(1234);
        }

        [Fact]
        public void Should_parse_global_persistentMessages()
        {
            const string connectionStringWithPersistenMessages = "host=localhost;persistentMessages=false";
            var connectionConfiguration = connectionStringParser.Parse(connectionStringWithPersistenMessages);

            connectionConfiguration.PersistentMessages.Should().BeFalse();
        }

        [Fact]
        public void Should_parse_global_timeout()
        {
            const string connectionStringWithTimeout = "host=localhost;timeout=13";
            var connectionConfiguration = connectionStringParser.Parse(connectionStringWithTimeout);

            connectionConfiguration.Timeout.Should().Be(13);
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

        [Fact]
        public void Should_Throw_Exception_OnInvalidAmqp()
        {
            Assert.Throws<EasyNetQException>(() => connectionStringParser.Parse("amqp=Foo"));
        }

        [Fact]
        public void Should_UsePort_From_ConnectionString()
        {
            var connectionConfiguration = connectionStringParser.Parse("amqp=amqp://host/;port=123");

            connectionConfiguration.Port.Should().Be(123);
        }
    }
}

// ReSharper restore InconsistentNaming
