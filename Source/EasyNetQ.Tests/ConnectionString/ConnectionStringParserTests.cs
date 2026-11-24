using EasyNetQ.ConnectionString;

namespace EasyNetQ.Tests.ConnectionString;

public class ConnectionStringParserTests
{
    private readonly ConnectionStringParser connectionStringParser = new();

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

    [Fact]
    public void Should_parse_host_with_port()
    {
        var configuration = connectionStringParser.Parse("host=my.host.com:1234");

        configuration.Hosts.Should().ContainSingle();
        configuration.Hosts[0].Host.Should().Be("my.host.com");
        configuration.Hosts[0].Port.Should().Be(1234);
    }

    [Fact]
    public void Should_parse_host_without_port()
    {
        var configuration = connectionStringParser.Parse("host=my.host.com");

        configuration.Hosts.Should().ContainSingle();
        configuration.Hosts[0].Host.Should().Be("my.host.com");
        configuration.Hosts[0].Port.Should().Be(0);
    }

    [Fact]
    public void Should_parse_list_of_hosts()
    {
        var configuration = connectionStringParser.Parse("host=host.one:1001,host.two:1002,host.three:1003");

        configuration.Hosts.Should().HaveCount(3);
        configuration.Hosts[0].Host.Should().Be("host.one");
        configuration.Hosts[0].Port.Should().Be(1001);
        configuration.Hosts[1].Host.Should().Be("host.two");
        configuration.Hosts[1].Port.Should().Be(1002);
        configuration.Hosts[2].Host.Should().Be("host.three");
        configuration.Hosts[2].Port.Should().Be(1003);
    }

    [Fact]
    public void Should_throw_when_parsing_empty()
    {
        Assert.Throws<EasyNetQException>(() => connectionStringParser.Parse(""));
    }

    [Theory]
    [InlineData("requestedHeartbeat=0")]
    [InlineData("requestedHeartbeat=-1")]
    public void Should_parse_zero_and_minus_one_heartbeat_as_infinite(string connectionString)
    {
        var configuration = connectionStringParser.Parse(connectionString);

        configuration.RequestedHeartbeat.Should().Be(Timeout.InfiniteTimeSpan);
    }
}
