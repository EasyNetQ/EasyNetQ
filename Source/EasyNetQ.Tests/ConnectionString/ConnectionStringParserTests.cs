// ReSharper disable InconsistentNaming

using System;
using System.Linq;
using EasyNetQ.ConnectionString;
using FluentAssertions;
using Xunit;

namespace EasyNetQ.Tests.ConnectionString;

public class ConnectionStringParserTests
{
    public ConnectionStringParserTests()
    {
        connectionStringParser = new ConnectionStringParser();
    }

    private readonly ConnectionStringParser connectionStringParser;

    private const string ConnectionString =
        "virtualHost=Copa;username=Copa;host=192.168.1.1;password=abc_xyz;port=12345;" +
        "requestedHeartbeat=3;prefetchcount=2;timeout=12;publisherConfirms=true;" +
        "name=unit-test;mandatoryPublish=true;consumerDispatcherConcurrency=1;ssl=true";

    [Fact]
    public void Should_correctly_parse_connection_string()
    {
        var configuration = connectionStringParser.Parse(ConnectionString);

        configuration.Hosts.First().Host.Should().Be("192.168.1.1");
        configuration.VirtualHost.Should().Be("Copa");
        configuration.UserName.Should().Be("Copa");
        configuration.Password.Should().Be("abc_xyz");
        configuration.Port.Should().Be(12345);
        configuration.RequestedHeartbeat.Should().Be(TimeSpan.FromSeconds(3));
        configuration.PrefetchCount.Should().Be(2);
        configuration.Timeout.Should().Be(TimeSpan.FromSeconds(12));
        configuration.PublisherConfirms.Should().BeTrue();
        configuration.Name.Should().Be("unit-test");
        configuration.MandatoryPublish.Should().BeTrue();
        configuration.ConsumerDispatcherConcurrency.Should().Be(1);
        configuration.Ssl.Enabled.Should().BeTrue();
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
}

// ReSharper restore InconsistentNaming
